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
using WalkyDoggy.Web.Infrastructure.MercadoPago;

namespace WalkyDoggy.Web.Controllers
{
    //Pagos con Mercado Pago. Todo el dinero entra a la cuenta de WalkyDoggy y queda retenido hasta que el paseo se hace.
    //Las acciones sobre una reserva exigen que quien llama haya iniciado sesion como el cliente dueño de la reserva.
    [RoutePrefix("api/payments")]
    public class PaymentsController : ApiControllerBase
    {
        private readonly IPaymentAppService paymentAppService;
        private readonly IEntityBaseRepository<Customer> customersRepository;
        private readonly IEntityBaseRepository<Walker> walkersRepository;

        public PaymentsController(IPaymentAppService paymentAppService,
                                  IEntityBaseRepository<Customer> customersRepository,
                                  IEntityBaseRepository<Walker> walkersRepository,
                                  IEntityBaseRepository<Error> errorsRepository,
                                  IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.paymentAppService = paymentAppService;
            this.customersRepository = customersRepository;
            this.walkersRepository = walkersRepository;
        }

        public class PaymentActionRequest
        {
            public String BookingKey { get; set; }

            public Int64 CustomerId { get; set; }

            //Solo para reclamos
            public String Reason { get; set; }
        }

        //Indica si los pagos con Mercado Pago estan configurados, para ofrecerlos o no al reservar
        [HttpGet]
        [Route("status")]
        public HttpResponseMessage Status(HttpRequestMessage request)
        {
            return CreateHttpResponse(request, () =>
                request.CreateResponse(HttpStatusCode.OK, new { enabled = MercadoPagoSettings.FromConfig().PaymentsConfigured }));
        }

        //Crea el pago en Mercado Pago y devuelve la direccion adonde mandar al cliente para que pague
        [HttpPost]
        [Route("checkout")]
        public HttpResponseMessage Checkout(HttpRequestMessage request, PaymentActionRequest action)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage denied;
                if (!CheckCustomer(request, action, out denied))
                {
                    return denied;
                }

                var returnUrl = request.RequestUri.GetLeftPart(UriPartial.Authority) + VirtualPathUtility.ToAbsolute("~/") + "api/payments/return";

                String error;
                var url = paymentAppService.CreateCheckout(action.BookingKey, action.CustomerId, returnUrl, out error);
                if (url == null)
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return request.CreateResponse(HttpStatusCode.OK, new { url });
            });
        }

        //Mercado Pago devuelve al cliente a esta direccion despues de pagar. Como llega por una redireccion del navegador
        //no trae la sesion: nada de lo que viene en la direccion se da por cierto, el pago se verifica contra Mercado Pago.
        [HttpGet]
        [Route("return")]
        public HttpResponseMessage Return(HttpRequestMessage request)
        {
            var query = request.GetQueryNameValuePairs().ToLookup(x => x.Key, x => x.Value);
            var bookingKey = query["external_reference"].FirstOrDefault();
            var paymentId = query["payment_id"].FirstOrDefault() ?? query["collection_id"].FirstOrDefault();
            if (paymentId == "null")
            {
                paymentId = null;
            }

            var result = "error";
            try
            {
                String error;
                result = paymentAppService.ConfirmPayment(bookingKey, paymentId, out error) ?? "error";
                if (error != null)
                {
                    LogError(new InvalidOperationException("Pago de la reserva " + bookingKey + ": " + error));
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }

            var response = request.CreateResponse(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri(request.RequestUri, VirtualPathUtility.ToAbsolute("~/") + "#/walks/requested?payment=" + result);
            return response;
        }

        //El cliente ya pago pero la app no se entero (por ejemplo, cerro la pagina antes de volver): se vuelve a verificar
        [HttpPost]
        [Route("sync")]
        public HttpResponseMessage Sync(HttpRequestMessage request, PaymentActionRequest action)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage denied;
                if (!CheckCustomer(request, action, out denied))
                {
                    return denied;
                }

                String error;
                var result = paymentAppService.ConfirmPayment(action.BookingKey, null, out error);
                if (result == null)
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return request.CreateResponse(HttpStatusCode.OK, new { result });
            });
        }

        [HttpPost]
        [Route("release")]
        public HttpResponseMessage Release(HttpRequestMessage request, PaymentActionRequest action)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage denied;
                if (!CheckCustomer(request, action, out denied))
                {
                    return denied;
                }

                String error;
                if (!paymentAppService.Release(action.BookingKey, action.CustomerId, out error))
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return request.CreateResponse(HttpStatusCode.OK, true);
            });
        }

        [HttpPost]
        [Route("dispute")]
        public HttpResponseMessage Dispute(HttpRequestMessage request, PaymentActionRequest action)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage denied;
                if (!CheckCustomer(request, action, out denied))
                {
                    return denied;
                }

                String error;
                if (!paymentAppService.Dispute(action.BookingKey, action.CustomerId, action.Reason, out error))
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return request.CreateResponse(HttpStatusCode.OK, true);
            });
        }

        //Cuanto tiene cobrado WalkyDoggy a nombre del paseador y en que etapa esta cada pago
        [HttpGet]
        [Route("walkerBalance")]
        public HttpResponseMessage WalkerBalance(HttpRequestMessage request, Int64 walkerId)
        {
            return CreateHttpResponse(request, () =>
            {
                if (!ActorIdentity.IsAuthenticated(User))
                {
                    return request.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
                }
                if (!ActorIdentity.IsWalker(User, walkersRepository, walkerId))
                {
                    return request.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés ver los cobros de otro paseador." });
                }

                return request.CreateResponse(HttpStatusCode.OK, paymentAppService.GetWalkerBalance(walkerId));
            });
        }

        private Boolean CheckCustomer(HttpRequestMessage request, PaymentActionRequest action, out HttpResponseMessage denied)
        {
            denied = null;

            if (action == null)
            {
                denied = request.CreateResponse(HttpStatusCode.BadRequest, new[] { "Faltan datos de la reserva." });
                return false;
            }
            if (!ActorIdentity.IsAuthenticated(User))
            {
                denied = request.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
                return false;
            }
            if (!ActorIdentity.IsCustomer(User, customersRepository, action.CustomerId))
            {
                denied = request.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés operar sobre la reserva de otro cliente." });
                return false;
            }

            return true;
        }
    }
}
