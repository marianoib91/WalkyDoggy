using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Application.Criterias;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/walks")]
    public class WalksController : ApiControllerBase
    {
        private readonly IWalkAppService walkAppService;
        private readonly IEntityBaseRepository<Customer> customersRepository;
        private readonly IEntityBaseRepository<Walker> walkersRepository;

        public WalksController(IWalkAppService walkAppService,
                                 IEntityBaseRepository<Customer> customersRepository,
                                 IEntityBaseRepository<Walker> walkersRepository,
                                 IEntityBaseRepository<Error> errorsRepository,
                                 IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.walkAppService = walkAppService;
            this.customersRepository = customersRepository;
            this.walkersRepository = walkersRepository;
        }

        //Confirmar o cancelar una reserva (cancelar puede devolver un pago) solo lo puede hacer quien inicio sesion como ese paseador o cliente
        private Boolean CheckActor(HttpRequestMessage request, BookingActionCriteria action, out HttpResponseMessage denied)
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

            var allowed = action.Actor == "Walker" ? ActorIdentity.IsWalker(User, walkersRepository, action.ActorId)
                        : action.Actor == "Customer" ? ActorIdentity.IsCustomer(User, customersRepository, action.ActorId)
                        : false;
            if (!allowed)
            {
                denied = request.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés operar sobre la reserva de otra persona." });
                return false;
            }

            return true;
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage request)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;

                var walksDto = this.walkAppService.GetAll();

                response = request.CreateResponse(HttpStatusCode.OK, walksDto);

                return response;
            });
        }

        [HttpGet]
        [Route("getAllByWalkerId")]
        public HttpResponseMessage GetAllByWalkerId(HttpRequestMessage request, Int64 walkerId)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;

                var walksDto = this.walkAppService.GetAllByWalkerId(walkerId);

                response = request.CreateResponse(HttpStatusCode.OK, walksDto);

                return response;
            });
        }

        [HttpGet]
        [Route("getAllForCurrentDay")]
        public HttpResponseMessage GetAllForCurrentDay(HttpRequestMessage request, Int64 walkerId)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;

                var walksDto = this.walkAppService.GetAllForCurrentDay(walkerId);

                response = request.CreateResponse(HttpStatusCode.OK, walksDto);

                return response;
            });
        }

        [HttpGet]
        [Route("getBookingsForWalker")]
        public HttpResponseMessage GetBookingsForWalker(HttpRequestMessage request, Int64 walkerId)
        {
            return CreateHttpResponse(request, () =>
            {
                var bookingsDto = this.walkAppService.GetBookingsForWalker(walkerId);

                return request.CreateResponse(HttpStatusCode.OK, bookingsDto);
            });
        }

        [HttpGet]
        [Route("getBookingsForCustomer")]
        public HttpResponseMessage GetBookingsForCustomer(HttpRequestMessage request, Int64 customerId)
        {
            return CreateHttpResponse(request, () =>
            {
                var bookingsDto = this.walkAppService.GetBookingsForCustomer(customerId);

                return request.CreateResponse(HttpStatusCode.OK, bookingsDto);
            });
        }

        [HttpPost]
        [Route("confirm")]
        public HttpResponseMessage Confirm(HttpRequestMessage request, BookingActionCriteria bookingActionCriteria)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage denied;
                if (!CheckActor(request, bookingActionCriteria, out denied))
                {
                    return denied;
                }

                String error;
                if (!this.walkAppService.Confirm(bookingActionCriteria, out error))
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return request.CreateResponse(HttpStatusCode.OK, true);
            });
        }

        [HttpPost]
        [Route("cancel")]
        public HttpResponseMessage Cancel(HttpRequestMessage request, BookingActionCriteria bookingActionCriteria)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage denied;
                if (!CheckActor(request, bookingActionCriteria, out denied))
                {
                    return denied;
                }

                String error;
                if (!this.walkAppService.Cancel(bookingActionCriteria, out error))
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return request.CreateResponse(HttpStatusCode.OK, true);
            });
        }

        [HttpPost]
        [Route("register")]
        public HttpResponseMessage Register(HttpRequestMessage request, WalkRequestCriteria walkRequestCriteria)
        {
            return CreateHttpResponse(request, () =>
            {
                String error;
                var walksDto = this.walkAppService.Register(walkRequestCriteria, out error);
                if (walksDto == null)
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return request.CreateResponse(HttpStatusCode.OK, walksDto);
            });
        }

        [HttpPost]
        [Route("validatePetsInWalks")]
        public HttpResponseMessage ValidatePetsInWalks(HttpRequestMessage request, AvailableWalkersCriteria availableWalkersCriteria)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;

                var walkDto = this.walkAppService.ValidatePetsInWalks(availableWalkersCriteria);

                response = request.CreateResponse(HttpStatusCode.OK, walkDto);

                return response;
            });
        }
    }
}