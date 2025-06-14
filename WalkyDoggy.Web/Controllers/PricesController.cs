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
    [RoutePrefix("api/prices")]
    public class PricesController : ApiControllerBase
    {
        private readonly IEntityBaseRepository<Price> pricesRepository;
        private readonly IMembershipService membershipService;
        private readonly IPriceAppService priceAppService;

        public PricesController(IEntityBaseRepository<Price> pricesRepository,
                                 IMembershipService membershipService,
                                 IPriceAppService priceAppService,
                                 IEntityBaseRepository<Error> errorsRepository,
                                 IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.pricesRepository = pricesRepository;
            this.membershipService = membershipService;
            this.priceAppService = priceAppService;
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage request)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;
                var pricesDto = this.priceAppService.GetAll();

                response = request.CreateResponse(HttpStatusCode.OK, pricesDto);

                return response;
            });
        }
    }
}