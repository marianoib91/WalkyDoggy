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
    [RoutePrefix("api/cities")]
    public class CitiesController : ApiControllerBase
    {
        private readonly IEntityBaseRepository<City> citiesRepository;
        private readonly IMembershipService membershipService;
        private readonly ICityAppService cityAppService;

        public CitiesController(IEntityBaseRepository<City> citiesRepository,
                                 IMembershipService membershipService,
                                 ICityAppService cityAppService,
                                 IEntityBaseRepository<Error> errorsRepository,
                                 IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.citiesRepository = citiesRepository;
            this.membershipService = membershipService;
            this.cityAppService = cityAppService;
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage request)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;
                var citiesDto = this.cityAppService.GetAll();

                response = request.CreateResponse(HttpStatusCode.OK, citiesDto);

                return response;
            });
        }

        [HttpPost]
        [Route("resolve")]
        public HttpResponseMessage Resolve(HttpRequestMessage request, CityResolveCriteria cityResolveCriteria)
        {
            return CreateHttpResponse(request, () =>
            {
                var cityDto = this.cityAppService.Resolve(cityResolveCriteria);
                if (cityDto == null)
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, new[] { "No se pudo reconocer la ciudad o la provincia de la dirección." });
                }

                return request.CreateResponse(HttpStatusCode.OK, cityDto);
            });
        }

        [HttpGet]
        [Route("getAllByProvinceId")]
        public HttpResponseMessage GetAllByProvinceId(HttpRequestMessage request, Int64 provinceId)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;
                var citiesDto = this.cityAppService.GetAllByProvinceId(provinceId);

                response = request.CreateResponse(HttpStatusCode.OK, citiesDto);

                return response;
            });
        }
    }
}