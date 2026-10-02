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
    public class RatingsController : ControladorApiBase
    {
        private readonly IServicioValoraciones servicioValoraciones;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;

        public RatingsController(IServicioValoraciones servicioValoraciones,
                                 IRepositorioEntidadBase<Customer> repositorioClientes,
                                 IRepositorioEntidadBase<Error> repositorioErrores,
                                 IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.servicioValoraciones = servicioValoraciones;
            this.repositorioClientes = repositorioClientes;
        }

        //Promedio, cantidad y desglose por estrellas
        [HttpGet]
        [Route("summary")]
        public HttpResponseMessage Summary(HttpRequestMessage pedido, Int64 walkerId)
        {
            return CrearRespuestaHttp(pedido, () => pedido.CreateResponse(HttpStatusCode.OK, servicioValoraciones.ObtenerResumen(walkerId)));
        }

        //Valoraciones de a paginas. sort: recent (por defecto), best o worst. stars: solo las de esa cantidad de estrellas.
        [HttpGet]
        [Route("list")]
        public HttpResponseMessage List(HttpRequestMessage pedido, Int64 walkerId, String sort = null, Int32? stars = null, Int32 page = 1, Int32 pageSize = 10)
        {
            return CrearRespuestaHttp(pedido, () => pedido.CreateResponse(HttpStatusCode.OK, servicioValoraciones.ObtenerValoraciones(walkerId, sort, stars, page, pageSize)));
        }

        [HttpPost]
        [Route("rate")]
        public HttpResponseMessage Rate(HttpRequestMessage pedido, RateRequestDto rate)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                if (rate == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "Faltan datos de la valoración." });
                }
                if (!IdentidadDelActor.EstaAutenticado(User))
                {
                    return pedido.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
                }
                if (!IdentidadDelActor.EsCliente(User, repositorioClientes, rate.CustomerId))
                {
                    return pedido.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés valorar un paseo de otro cliente." });
                }

                String error;
                if (!servicioValoraciones.Valorar(rate, out error))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return pedido.CreateResponse(HttpStatusCode.OK, true);
            });
        }
    }
}
