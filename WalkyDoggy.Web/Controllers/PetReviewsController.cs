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
    //Reseñas que los paseadores dejan de las mascotas que llevaron. Solo las pueden ver y escribir los paseadores.
    [RoutePrefix("api/petReviews")]
    public class PetReviewsController : ControladorApiBase
    {
        //Cuantas reseñas se traen como maximo al pedir el detalle de una mascota
        private const Int32 MaximoDeResenas = 20;

        private readonly IServicioResenasMascotas servicioResenas;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;

        public PetReviewsController(IServicioResenasMascotas servicioResenas,
                                    IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                    IRepositorioEntidadBase<Error> repositorioErrores,
                                    IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.servicioResenas = servicioResenas;
            this.repositorioPaseadores = repositorioPaseadores;
        }

        private Boolean VerificarPaseador(HttpRequestMessage pedido, Int64 idPaseador, out HttpResponseMessage denegado)
        {
            denegado = null;

            if (!IdentidadDelActor.EstaAutenticado(User))
            {
                denegado = pedido.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
                return false;
            }
            if (!IdentidadDelActor.EsPaseador(User, repositorioPaseadores, idPaseador))
            {
                denegado = pedido.CreateResponse(HttpStatusCode.Forbidden, new[] { "Las reseñas de las mascotas son solo para paseadores." });
                return false;
            }

            return true;
        }

        //Resumen y ultimas reseñas de una mascota; las del paseador que consulta vienen marcadas como propias
        [HttpGet]
        [Route("list")]
        public HttpResponseMessage List(HttpRequestMessage pedido, Int64 petId, Int64 walkerId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage denegado;
                if (!VerificarPaseador(pedido, walkerId, out denegado))
                {
                    return denegado;
                }

                return pedido.CreateResponse(HttpStatusCode.OK, this.servicioResenas.ObtenerResenas(petId, walkerId, MaximoDeResenas));
            });
        }

        [HttpPost]
        [Route("rate")]
        public HttpResponseMessage Rate(HttpRequestMessage pedido, PetReviewRequestDto solicitud)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                if (solicitud == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "Faltan datos de la reseña." });
                }

                HttpResponseMessage denegado;
                if (!VerificarPaseador(pedido, solicitud.WalkerId, out denegado))
                {
                    return denegado;
                }

                String error;
                if (!this.servicioResenas.Resenar(solicitud, out error))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return pedido.CreateResponse(HttpStatusCode.OK, true);
            });
        }
    }
}
