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
using WalkyDoggy.Entities.GoogleAPI;
using WalkyDoggy.Services;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/geocode")]
    public class GeocodeController : ApiControllerBase
    {
        private readonly IGeocodeAppService geocodeAppService;

        public GeocodeController(IGeocodeAppService geocodeAppService,
                                 IEntityBaseRepository<Error> errorsRepository,
                                 IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.geocodeAppService = geocodeAppService;
        }


        [HttpPost]
        [Route("getLocation")]
        public HttpResponseMessage GetLocation(HttpRequestMessage request, Geocode geocode)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;
                var geocodeDto = this.geocodeAppService.GetLocation(geocode);

                response = request.CreateResponse(HttpStatusCode.OK, geocodeDto);

                return response;
            });
        }
    }
}