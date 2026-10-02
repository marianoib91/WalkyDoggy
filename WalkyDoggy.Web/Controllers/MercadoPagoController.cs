using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Web.Infrastructure.Core;
using WalkyDoggy.Web.Infrastructure.MercadoPago;

namespace WalkyDoggy.Web.Controllers
{
    //Vinculacion de la cuenta de Mercado Pago del paseador (OAuth): los clientes le pagan directamente a su cuenta.
    //Los endpoints que leen o cambian el vinculo exigen que quien llama haya iniciado sesion como ese paseador.
    [RoutePrefix("api/mercadopago")]
    public class MercadoPagoController : ControladorApiBase
    {
        private const String PropositoState = "WalkyDoggy.MercadoPago.State";
        private const String TokenPurpose = ProveedorTokensVendedor.TokenPurpose;
        private static readonly TimeSpan DuracionState = TimeSpan.FromMinutes(15);

        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;

        public MercadoPagoController(IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                     IRepositorioEntidadBase<Error> repositorioErrores,
                                     IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.repositorioPaseadores = repositorioPaseadores;
        }

        public class WalkerActionRequest
        {
            public Int64 WalkerId { get; set; }
        }

        [HttpGet]
        [Route("status")]
        public HttpResponseMessage Status(HttpRequestMessage pedido, Int64 walkerId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage denegado;
                var paseador = BuscarPaseadorPropio(pedido, walkerId, out denegado);
                if (paseador == null)
                {
                    return denegado;
                }

                var configuracion = ConfiguracionMercadoPago.DesdeConfiguracion();

                return pedido.CreateResponse(HttpStatusCode.OK, new
                {
                    configured = configuracion.IsConfigured,
                    linked = paseador.MercadoPagoAccessToken != null,
                    userId = paseador.MercadoPagoUserId,
                    linkedAt = paseador.MercadoPagoLinkedAt
                });
            });
        }

        //Devuelve la direccion de Mercado Pago a la que hay que mandar al paseador para que autorice la vinculacion
        [HttpGet]
        [Route("authorizationUrl")]
        public HttpResponseMessage AuthorizationUrl(HttpRequestMessage pedido, Int64 walkerId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage denegado;
                var paseador = BuscarPaseadorPropio(pedido, walkerId, out denegado);
                if (paseador == null)
                {
                    return denegado;
                }

                var configuracion = ConfiguracionMercadoPago.DesdeConfiguracion();
                if (!configuracion.IsConfigured)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest,
                        new[] { "La vinculación con Mercado Pago todavía no está configurada en el sistema." });
                }

                //El "state" identifica al paseador y vence a los 15 minutos; se verifica al volver de Mercado Pago
                //El "state" lleva adelante a que servidor local volver ("h8085~" = http://localhost:8085): Mercado Pago devuelve al
                //paseador a la pagina puente publica y esta lo reenvia a esta aplicacion usando ese dato
                var origen = (pedido.RequestUri.Scheme == Uri.UriSchemeHttps ? "s" : "h") + pedido.RequestUri.Port + "~";
                var state = origen + ProtectorDeSecretos.ProtegerParaUrl(paseador.Id + "|" + DateTime.UtcNow.Add(DuracionState).Ticks, PropositoState);
                var url = new ClienteOAuthMercadoPago(configuracion).ArmarUrlDeAutorizacion(state);

                return pedido.CreateResponse(HttpStatusCode.OK, new { url });
            });
        }

        //Mercado Pago manda al paseador de vuelta a esta direccion con el codigo de autorizacion.
        //Como llega por una redireccion del navegador no trae la sesion: se valida con el "state" firmado.
        [HttpGet]
        [Route("callback")]
        public async Task<HttpResponseMessage> Callback(HttpRequestMessage pedido, String code = null, String state = null, String error = null)
        {
            var resultado = "error";

            try
            {
                var configuracion = ConfiguracionMercadoPago.DesdeConfiguracion();

                if (!configuracion.IsConfigured)
                {
                    resultado = "error";
                }
                else if (!String.IsNullOrEmpty(error) || String.IsNullOrEmpty(code))
                {
                    resultado = "denied";
                }
                else
                {
                    var idPaseador = LeerState(state);
                    var paseador = idPaseador.HasValue ? repositorioPaseadores.ObtenerUno(idPaseador.Value) : null;

                    if (paseador != null)
                    {
                        var tokens = await new ClienteOAuthMercadoPago(configuracion).CanjearCodigoAsync(code);

                        paseador.MercadoPagoUserId = tokens.UserId;
                        paseador.MercadoPagoAccessToken = ProtectorDeSecretos.Proteger(tokens.AccessToken, TokenPurpose);
                        paseador.MercadoPagoRefreshToken = tokens.RefreshToken == null ? null : ProtectorDeSecretos.Proteger(tokens.RefreshToken, TokenPurpose);
                        paseador.MercadoPagoPublicKey = tokens.PublicKey;
                        paseador.MercadoPagoTokenExpiresAt = tokens.ExpiresAtUtc;
                        paseador.MercadoPagoLinkedAt = DateTime.UtcNow;
                        _unidadDeTrabajo.GuardarCambios();

                        resultado = "linked";
                    }
                }
            }
            catch (Exception ex)
            {
                RegistrarError(ex);
                resultado = "error";
            }

            var respuesta = pedido.CreateResponse(HttpStatusCode.Redirect);
            respuesta.Headers.Location = new Uri(pedido.RequestUri, VirtualPathUtility.ToAbsolute("~/") + "#/profile?mp=" + resultado);
            return respuesta;
        }

        [HttpPost]
        [Route("unlink")]
        public HttpResponseMessage Unlink(HttpRequestMessage pedido, WalkerActionRequest pedidoAccion)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage denegado;
                var paseador = BuscarPaseadorPropio(pedido, pedidoAccion == null ? 0 : pedidoAccion.WalkerId, out denegado);
                if (paseador == null)
                {
                    return denegado;
                }

                paseador.MercadoPagoUserId = null;
                paseador.MercadoPagoAccessToken = null;
                paseador.MercadoPagoRefreshToken = null;
                paseador.MercadoPagoPublicKey = null;
                paseador.MercadoPagoTokenExpiresAt = null;
                paseador.MercadoPagoLinkedAt = null;
                _unidadDeTrabajo.GuardarCambios();

                return pedido.CreateResponse(HttpStatusCode.OK, true);
            });
        }

        //Solo el propio paseador puede ver o cambiar su vinculo
        private Walker BuscarPaseadorPropio(HttpRequestMessage pedido, Int64 idPaseador, out HttpResponseMessage denegado)
        {
            denegado = null;

            if (User == null || User.Identity == null || !User.Identity.IsAuthenticated)
            {
                denegado = pedido.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
                return null;
            }

            var paseador = repositorioPaseadores.TodosConIncluidos(x => x.User).FirstOrDefault(x => x.Id == idPaseador);
            if (paseador == null || paseador.User == null ||
                !String.Equals(paseador.User.Email, User.Identity.Name, StringComparison.OrdinalIgnoreCase))
            {
                denegado = pedido.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés modificar la cuenta de otro paseador." });
                return null;
            }

            return paseador;
        }

        //Devuelve el id del paseador que inicio la vinculacion, o null si el "state" fue alterado o ya venció
        private static Int64? LeerState(String state)
        {
            if (String.IsNullOrEmpty(state))
            {
                return null;
            }

            //Se descarta el prefijo "h8085~" que solo usa la pagina puente para saber a donde volver
            var separador = state.IndexOf('~');
            var textoPlano = ProtectorDeSecretos.DesprotegerDeUrl(separador >= 0 ? state.Substring(separador + 1) : state, PropositoState);
            if (textoPlano == null)
            {
                return null;
            }

            var partes = textoPlano.Split('|');
            Int64 idPaseador, ticksDeVencimiento;
            if (partes.Length != 2 || !Int64.TryParse(partes[0], out idPaseador) || !Int64.TryParse(partes[1], out ticksDeVencimiento))
            {
                return null;
            }

            return DateTime.UtcNow.Ticks > ticksDeVencimiento ? (Int64?)null : idPaseador;
        }
    }
}
