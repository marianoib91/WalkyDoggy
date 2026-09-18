using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Application.Criterias;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/walkers")]
    public class WalkersController : ApiControllerBase
    {
        private readonly IEntityBaseRepository<Walker> walkersRepository;
        private readonly IMembershipService membershipService;
        private readonly IWalkerAppService walkerAppService;

        public WalkersController(IEntityBaseRepository<Walker> walkersRepository,
                                 IMembershipService membershipService,
                                 IWalkerAppService walkerAppService,
                                 IEntityBaseRepository<Error> errorsRepository,
                                 IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.walkersRepository = walkersRepository;
            this.membershipService = membershipService;
            this.walkerAppService = walkerAppService;
        }

        [HttpPost]
        [Route("register")]
        public HttpResponseMessage Register(HttpRequestMessage request, WalkerDto walkerDto)
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
                    if (membershipService.UserExists(walkerDto.Email))
                    {
                        ModelState.AddModelError("E-mail invalido", "El email ingresado ya se encuentra en uso.");
                        response = request.CreateResponse(HttpStatusCode.BadRequest,
                        ModelState.Keys.SelectMany(k => ModelState[k].Errors)
                              .Select(m => m.ErrorMessage).ToArray());
                    }
                    else
                    {
                        var walker = this.walkerAppService.Register(walkerDto);
                        response = request.CreateResponse<WalkerDto>(HttpStatusCode.OK, walker);
                    }
                }

                return response;
            });
        }

        [HttpGet]
        [Route("getByUserId")]
        public HttpResponseMessage GetByUserId(HttpRequestMessage request, Int64 userId)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;
                var walkerDto = this.walkerAppService.GetByUserId(userId);

                response = request.CreateResponse(HttpStatusCode.OK, walkerDto);

                return response;
            });
        }

        [HttpPost]
        [Route("getAvailableWalkers")]
        public HttpResponseMessage GetAvailableWalkers(HttpRequestMessage request, AvailableWalkersCriteria availableWalkersCriteria)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;
                var walkersDto = this.walkerAppService.GetAvailableWalkers(availableWalkersCriteria);
                response = request.CreateResponse(HttpStatusCode.OK, walkersDto);
                return response;
            });
        }

        [HttpPost]
        [Route("update")]
        public HttpResponseMessage Update(HttpRequestMessage request, WalkerDto walkerDto)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;
                this.walkerAppService.Update(walkerDto);

                response = request.CreateResponse(HttpStatusCode.OK, walkerDto);

                return response;
            });
        }

        [HttpGet]
        [Route("getDetail")]
        public HttpResponseMessage GetDetail(HttpRequestMessage request, Int64 id)
        {
            return CreateHttpResponse(request, () =>
            {
                var walkerDto = this.walkerAppService.GetDetail(id);
                if (walkerDto == null)
                {
                    return request.CreateResponse(HttpStatusCode.NotFound, "El paseador no existe.");
                }

                return request.CreateResponse(HttpStatusCode.OK, walkerDto);
            });
        }

        [HttpGet]
        [Route("getAvailableTimes")]
        public HttpResponseMessage GetAvailableTimes(HttpRequestMessage request, Int64 walkerId, String date)
        {
            return CreateHttpResponse(request, () =>
            {
                DateTime parsedDate;
                if (!DateTime.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                                            System.Globalization.DateTimeStyles.None, out parsedDate))
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, new[] { "La fecha no es válida." });
                }

                var times = this.walkerAppService.GetAvailableTimes(walkerId, parsedDate);

                return request.CreateResponse(HttpStatusCode.OK, times);
            });
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage request)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;

                var walkersDto = this.walkerAppService.GetAll();

                response = request.CreateResponse(HttpStatusCode.OK, walkersDto);

                return response;
            });
        }
    }
}