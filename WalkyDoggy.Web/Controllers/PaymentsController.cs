using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    //Pago de un paseo con Mercado Pago. Se paga despues del paseo, cuando el paseador lo dio por finalizado, y el dinero va
    //directo a la cuenta de Mercado Pago del paseador. Las acciones sobre una reserva exigen que quien llama haya iniciado
    //sesion como el cliente dueño de la reserva.
    [RoutePrefix("api/payments")]
    public class PaymentsController : ControladorApiBase
    {
        private readonly IServicioPagos servicioPagos;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;

        public PaymentsController(IServicioPagos servicioPagos,
                                  IRepositorioEntidadBase<Customer> repositorioClientes,
                                  IRepositorioEntidadBase<Error> repositorioErrores,
                                  IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.servicioPagos = servicioPagos;
            this.repositorioClientes = repositorioClientes;
        }

        public class PaymentActionRequest
        {
            public String BookingKey { get; set; }

            public Int64 CustomerId { get; set; }
        }

        //Crea el pago en Mercado Pago y devuelve la direccion adonde mandar al cliente para que pague
        [HttpPost]
        [Route("checkout")]
        public HttpResponseMessage Checkout(HttpRequestMessage pedido, PaymentActionRequest accion)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage denegado;
                if (!VerificarCliente(pedido, accion, out denegado))
                {
                    return denegado;
                }

                var urlDeRetorno = pedido.RequestUri.GetLeftPart(UriPartial.Authority) + VirtualPathUtility.ToAbsolute("~/") + "api/payments/return";

                String error;
                var url = servicioPagos.CrearCheckout(accion.BookingKey, accion.CustomerId, urlDeRetorno, out error);
                if (url == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return pedido.CreateResponse(HttpStatusCode.OK, new { url });
            });
        }

        //Mercado Pago devuelve al cliente a esta direccion despues de pagar. Como llega por una redireccion del navegador
        //no trae la sesion: nada de lo que viene en la direccion se da por cierto, el pago se verifica contra Mercado Pago.
        [HttpGet]
        [Route("return")]
        public HttpResponseMessage Return(HttpRequestMessage pedido)
        {
            var consulta = pedido.GetQueryNameValuePairs().ToLookup(x => x.Key, x => x.Value);
            var claveReserva = consulta["external_reference"].FirstOrDefault();
            var idPago = consulta["payment_id"].FirstOrDefault() ?? consulta["collection_id"].FirstOrDefault();
            if (idPago == "null")
            {
                idPago = null;
            }

            var resultado = "error";
            try
            {
                String error;
                resultado = servicioPagos.ConfirmarPago(claveReserva, idPago, out error) ?? "error";
                if (error != null)
                {
                    RegistrarError(new InvalidOperationException("Pago de la reserva " + claveReserva + ": " + error));
                }
            }
            catch (Exception ex)
            {
                RegistrarError(ex);
            }

            var respuesta = pedido.CreateResponse(HttpStatusCode.Redirect);
            respuesta.Headers.Location = new Uri(pedido.RequestUri, VirtualPathUtility.ToAbsolute("~/") + "#/walks/requested?payment=" + resultado);
            return respuesta;
        }

        //El cliente ya pago pero la app no se entero (por ejemplo, no volvio a la app despues de pagar): se vuelve a verificar
        [HttpPost]
        [Route("sync")]
        public HttpResponseMessage Sync(HttpRequestMessage pedido, PaymentActionRequest accion)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage denegado;
                if (!VerificarCliente(pedido, accion, out denegado))
                {
                    return denegado;
                }

                String error;
                var resultado = servicioPagos.ConfirmarPago(accion.BookingKey, null, out error);
                if (resultado == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return pedido.CreateResponse(HttpStatusCode.OK, new { result = resultado });
            });
        }

        private Boolean VerificarCliente(HttpRequestMessage pedido, PaymentActionRequest accion, out HttpResponseMessage denegado)
        {
            denegado = null;

            if (accion == null)
            {
                denegado = pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "Faltan datos de la reserva." });
                return false;
            }
            if (!IdentidadDelActor.EstaAutenticado(User))
            {
                denegado = pedido.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
                return false;
            }
            if (!IdentidadDelActor.EsCliente(User, repositorioClientes, accion.CustomerId))
            {
                denegado = pedido.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés operar sobre la reserva de otro cliente." });
                return false;
            }

            return true;
        }
    }
}
