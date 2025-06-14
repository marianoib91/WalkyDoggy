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
using WalkyDoggy.Services;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/provinces")]
    public class ProvincesController : ApiControllerBase
    {
        private readonly IEntityBaseRepository<Province> provincesRepository;
        private readonly IMembershipService membershipService;
        private readonly IProvinceAppService provinceAppService;

        public ProvincesController(IEntityBaseRepository<Province> provincesRepository,
                                 IMembershipService membershipService,
                                 IProvinceAppService provinceAppService,
                                 IEntityBaseRepository<Error> errorsRepository,
                                 IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.provincesRepository = provincesRepository;
            this.membershipService = membershipService;
            this.provinceAppService = provinceAppService;
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage request)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;
                var provincesDto = this.provinceAppService.GetAll();

                response = request.CreateResponse(HttpStatusCode.OK, provincesDto);

                return response;
            });
        }
    }
}