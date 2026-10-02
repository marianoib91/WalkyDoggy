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
    //Vinculacion de la cuenta de Mercado Pago del paseador (OAuth) para que cobre los pagos online directamente.
    //Los endpoints que leen o cambian el vinculo exigen que quien llama haya iniciado sesion como ese paseador.
    [RoutePrefix("api/mercadopago")]
    public class MercadoPagoController : ApiControllerBase
    {
        private const String StatePurpose = "WalkyDoggy.MercadoPago.State";
        private const String TokenPurpose = "WalkyDoggy.MercadoPago.Token";
        private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(15);

        private readonly IEntityBaseRepository<Walker> walkersRepository;

        public MercadoPagoController(IEntityBaseRepository<Walker> walkersRepository,
                                     IEntityBaseRepository<Error> errorsRepository,
                                     IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.walkersRepository = walkersRepository;
        }

        public class WalkerActionRequest
        {
            public Int64 WalkerId { get; set; }
        }

        [HttpGet]
        [Route("status")]
        public HttpResponseMessage Status(HttpRequestMessage request, Int64 walkerId)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage denied;
                var walker = FindOwnWalker(request, walkerId, out denied);
                if (walker == null)
                {
                    return denied;
                }

                var settings = MercadoPagoSettings.FromConfig();

                return request.CreateResponse(HttpStatusCode.OK, new
                {
                    //La vinculacion esta en desuso: los pagos los cobra WalkyDoggy. Solo se ofrece si se la habilita en la configuracion.
                    enabled = settings.LinkingEnabled,
                    configured = settings.IsConfigured,
                    linked = walker.MercadoPagoAccessToken != null,
                    userId = walker.MercadoPagoUserId,
                    linkedAt = walker.MercadoPagoLinkedAt
                });
            });
        }

        //Devuelve la direccion de Mercado Pago a la que hay que mandar al paseador para que autorice la vinculacion
        [HttpGet]
        [Route("authorizationUrl")]
        public HttpResponseMessage AuthorizationUrl(HttpRequestMessage request, Int64 walkerId)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage denied;
                var walker = FindOwnWalker(request, walkerId, out denied);
                if (walker == null)
                {
                    return denied;
                }

                var settings = MercadoPagoSettings.FromConfig();
                if (!settings.IsConfigured)
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest,
                        new[] { "La vinculación con Mercado Pago todavía no está configurada en el sistema." });
                }

                //El "state" identifica al paseador y vence a los 15 minutos; se verifica al volver de Mercado Pago
                var state = SecretProtector.ProtectForUrl(walker.Id + "|" + DateTime.UtcNow.Add(StateLifetime).Ticks, StatePurpose);
                var url = new MercadoPagoOAuthClient(settings).BuildAuthorizationUrl(state);

                return request.CreateResponse(HttpStatusCode.OK, new { url });
            });
        }

        //Mercado Pago manda al paseador de vuelta a esta direccion con el codigo de autorizacion.
        //Como llega por una redireccion del navegador no trae la sesion: se valida con el "state" firmado.
        [HttpGet]
        [Route("callback")]
        public async Task<HttpResponseMessage> Callback(HttpRequestMessage request, String code = null, String state = null, String error = null)
        {
            var result = "error";

            try
            {
                var settings = MercadoPagoSettings.FromConfig();

                if (!settings.IsConfigured)
                {
                    result = "error";
                }
                else if (!String.IsNullOrEmpty(error) || String.IsNullOrEmpty(code))
                {
                    result = "denied";
                }
                else
                {
                    var walkerId = ReadState(state);
                    var walker = walkerId.HasValue ? walkersRepository.GetSingle(walkerId.Value) : null;

                    if (walker != null)
                    {
                        var tokens = await new MercadoPagoOAuthClient(settings).ExchangeCodeAsync(code);

                        walker.MercadoPagoUserId = tokens.UserId;
                        walker.MercadoPagoAccessToken = SecretProtector.Protect(tokens.AccessToken, TokenPurpose);
                        walker.MercadoPagoRefreshToken = tokens.RefreshToken == null ? null : SecretProtector.Protect(tokens.RefreshToken, TokenPurpose);
                        walker.MercadoPagoPublicKey = tokens.PublicKey;
                        walker.MercadoPagoTokenExpiresAt = tokens.ExpiresAtUtc;
                        walker.MercadoPagoLinkedAt = DateTime.UtcNow;
                        _unitOfWork.Commit();

                        result = "linked";
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                result = "error";
            }

            var response = request.CreateResponse(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri(request.RequestUri, VirtualPathUtility.ToAbsolute("~/") + "#/profile?mp=" + result);
            return response;
        }

        [HttpPost]
        [Route("unlink")]
        public HttpResponseMessage Unlink(HttpRequestMessage request, WalkerActionRequest actionRequest)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage denied;
                var walker = FindOwnWalker(request, actionRequest == null ? 0 : actionRequest.WalkerId, out denied);
                if (walker == null)
                {
                    return denied;
                }

                walker.MercadoPagoUserId = null;
                walker.MercadoPagoAccessToken = null;
                walker.MercadoPagoRefreshToken = null;
                walker.MercadoPagoPublicKey = null;
                walker.MercadoPagoTokenExpiresAt = null;
                walker.MercadoPagoLinkedAt = null;
                _unitOfWork.Commit();

                return request.CreateResponse(HttpStatusCode.OK, true);
            });
        }

        //Solo el propio paseador puede ver o cambiar su vinculo
        private Walker FindOwnWalker(HttpRequestMessage request, Int64 walkerId, out HttpResponseMessage denied)
        {
            denied = null;

            if (User == null || User.Identity == null || !User.Identity.IsAuthenticated)
            {
                denied = request.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
                return null;
            }

            var walker = walkersRepository.AllIncluding(x => x.User).FirstOrDefault(x => x.Id == walkerId);
            if (walker == null || walker.User == null ||
                !String.Equals(walker.User.Email, User.Identity.Name, StringComparison.OrdinalIgnoreCase))
            {
                denied = request.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés modificar la cuenta de otro paseador." });
                return null;
            }

            return walker;
        }

        //Devuelve el id del paseador que inicio la vinculacion, o null si el "state" fue alterado o ya venció
        private static Int64? ReadState(String state)
        {
            if (String.IsNullOrEmpty(state))
            {
                return null;
            }

            var plain = SecretProtector.UnprotectFromUrl(state, StatePurpose);
            if (plain == null)
            {
                return null;
            }

            var parts = plain.Split('|');
            Int64 walkerId, expiresTicks;
            if (parts.Length != 2 || !Int64.TryParse(parts[0], out walkerId) || !Int64.TryParse(parts[1], out expiresTicks))
            {
                return null;
            }

            return DateTime.UtcNow.Ticks > expiresTicks ? (Int64?)null : walkerId;
        }
    }
}
