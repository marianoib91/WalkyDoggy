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
    [RoutePrefix("api/breeds")]
    public class BreedsController : ApiControllerBase
    {
        private readonly IEntityBaseRepository<Breed> breedsRepository;
        private readonly IMembershipService membershipService;
        private readonly IBreedAppService breedAppService;

        public BreedsController(IEntityBaseRepository<Breed> breedsRepository,
                                 IMembershipService membershipService,
                                 IBreedAppService breedAppService,
                                 IEntityBaseRepository<Error> errorsRepository,
                                 IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.breedsRepository = breedsRepository;
            this.membershipService = membershipService;
            this.breedAppService = breedAppService;
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage request)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;
                var breedsDto = this.breedAppService.GetAll();

                response = request.CreateResponse(HttpStatusCode.OK, breedsDto);

                return response;
            });
        }
    }
}