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

        public WalksController(IWalkAppService walkAppService,
                                 IEntityBaseRepository<Error> errorsRepository,
                                 IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.walkAppService = walkAppService;
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