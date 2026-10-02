using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    //Valoraciones de los paseadores. Verlas es publico; valorar exige haber iniciado sesion como el cliente de la reserva.
    [RoutePrefix("api/ratings")]
    public class RatingsController : ApiControllerBase
    {
        private readonly IRatingAppService ratingAppService;
        private readonly IEntityBaseRepository<Customer> customersRepository;

        public RatingsController(IRatingAppService ratingAppService,
                                 IEntityBaseRepository<Customer> customersRepository,
                                 IEntityBaseRepository<Error> errorsRepository,
                                 IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.ratingAppService = ratingAppService;
            this.customersRepository = customersRepository;
        }

        //Promedio, cantidad y desglose por estrellas
        [HttpGet]
        [Route("summary")]
        public HttpResponseMessage Summary(HttpRequestMessage request, Int64 walkerId)
        {
            return CreateHttpResponse(request, () => request.CreateResponse(HttpStatusCode.OK, ratingAppService.GetSummary(walkerId)));
        }

        //Valoraciones de a paginas. sort: recent (por defecto), best o worst. stars: solo las de esa cantidad de estrellas.
        [HttpGet]
        [Route("list")]
        public HttpResponseMessage List(HttpRequestMessage request, Int64 walkerId, String sort = null, Int32? stars = null, Int32 page = 1, Int32 pageSize = 10)
        {
            return CreateHttpResponse(request, () => request.CreateResponse(HttpStatusCode.OK, ratingAppService.GetRatings(walkerId, sort, stars, page, pageSize)));
        }

        [HttpPost]
        [Route("rate")]
        public HttpResponseMessage Rate(HttpRequestMessage request, RateRequestDto rate)
        {
            return CreateHttpResponse(request, () =>
            {
                if (rate == null)
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, new[] { "Faltan datos de la valoración." });
                }
                if (!ActorIdentity.IsAuthenticated(User))
                {
                    return request.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
                }
                if (!ActorIdentity.IsCustomer(User, customersRepository, rate.CustomerId))
                {
                    return request.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés valorar un paseo de otro cliente." });
                }

                String error;
                if (!ratingAppService.Rate(rate, out error))
                {
                    return request.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return request.CreateResponse(HttpStatusCode.OK, true);
            });
        }
    }
}
