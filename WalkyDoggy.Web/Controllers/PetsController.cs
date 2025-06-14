using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/pets")]
    public class PetsController : ApiControllerBase
    {
        private readonly IEntityBaseRepository<Pet> petsRepository;
        private readonly IMembershipService membershipService;
        private readonly IPetAppService petAppService;

        public PetsController(IEntityBaseRepository<Pet> petsRepository,
                                 IMembershipService membershipService,
                                 IPetAppService petAppService,
                                 IEntityBaseRepository<Error> errorsRepository,
                                 IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.petsRepository = petsRepository;
            this.membershipService = membershipService;
            this.petAppService = petAppService;
        }

        [HttpPost]
        [Route("register")]
        public HttpResponseMessage Register(HttpRequestMessage request, PetDto petDto)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                if (!ModelState.IsValid)
                {
                    response = request.CreateResponse(HttpStatusCode.BadRequest,
                        ModelState.Keys.SelectMany(k => ModelState[k].Errors)
                              .Select(m => m.ErrorMessage).ToArray());
                }
                else
                {
                    var pet = this.petAppService.Register(petDto);
                    response = request.CreateResponse<Pet>(HttpStatusCode.OK, pet);

                }

                return response;
            });
        }
        [HttpPost]
        [Route("update")]
        public HttpResponseMessage Update(HttpRequestMessage request, PetDto petDto)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                if (!ModelState.IsValid)
                {
                    response = request.CreateResponse(HttpStatusCode.BadRequest,
                        ModelState.Keys.SelectMany(k => ModelState[k].Errors)
                              .Select(m => m.ErrorMessage).ToArray());
                }
                else
                {
                    var pet = this.petAppService.Update(petDto);
                    response = request.CreateResponse<Pet>(HttpStatusCode.OK, pet);

                }

                return response;
            });
        }

        [HttpGet]
        [Route("getById")]
        public HttpResponseMessage GetById(HttpRequestMessage request, Int64 id)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;
                var petDto = this.petAppService.GetById(id);

                response = request.CreateResponse(HttpStatusCode.OK, petDto);

                return response;
            });
        }


        [HttpGet]
        [Route("getAllByCustomerId")]
        public HttpResponseMessage GetAllByCustomerId(HttpRequestMessage request, Int64 customerId)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;
                var petsDto = this.petAppService.GetAllByCustomerId(customerId);

                response = request.CreateResponse(HttpStatusCode.OK, petsDto);

                return response;
            });
        }

        public HttpResponseMessage Delete(HttpRequestMessage request, Int64 id)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var pet = petsRepository.GetSingle(id);
                petsRepository.Delete(pet);
                _unitOfWork.Commit();
                response = request.CreateResponse(HttpStatusCode.OK, pet);
                return response;
            });
        }
    }
}