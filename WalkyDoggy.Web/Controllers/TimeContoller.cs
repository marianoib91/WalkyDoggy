using System;
using System.Collections.Generic;
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
    //TODO: Ver porque no anda este controller
    [RoutePrefix("api/times")]
    public class TimeContoller : ApiControllerBase
    {
        private readonly ITimeAppService timeAppService;

        public TimeContoller(ITimeAppService timeAppService,
                             IEntityBaseRepository<Error> errorsRepository,
                             IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.timeAppService = timeAppService;
        }

        [HttpGet]
        [Route("getAvailableWalkTimes")]
        public HttpResponseMessage GetAvailableWalkTimes(HttpRequestMessage request)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var timesDto = this.timeAppService.GetAvailableWalkTimes();
                response = request.CreateResponse(HttpStatusCode.OK, timesDto);

                return response;
            });
        }

        [HttpGet]
        [Route("getAllWalkTimes")]
        public HttpResponseMessage GetAllWalkTimes(HttpRequestMessage request)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var timesDto = this.timeAppService.GetAllWalkTimes();
                response = request.CreateResponse(HttpStatusCode.OK, timesDto);

                return response;
            });
        }
    }
}