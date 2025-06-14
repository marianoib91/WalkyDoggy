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
    [RoutePrefix("api/sizes")]
    public class SizesController : ApiControllerBase
    {
        private readonly IEntityBaseRepository<Size> sizesRepository;
        private readonly IMembershipService membershipService;
        private readonly ISizeAppService sizeAppService;

        public SizesController(IEntityBaseRepository<Size> sizesRepository,
                                 IMembershipService membershipService,
                                 ISizeAppService sizeAppService,
                                 IEntityBaseRepository<Error> errorsRepository,
                                 IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.sizesRepository = sizesRepository;
            this.membershipService = membershipService;
            this.sizeAppService = sizeAppService;
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage request)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;
                var sizesDto = this.sizeAppService.GetAll();

                response = request.CreateResponse(HttpStatusCode.OK, sizesDto);

                return response;
            });
        }
    }
}