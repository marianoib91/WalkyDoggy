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
    //Moderacion de reseñas: el administrador oculta (y vuelve a mostrar) valoraciones de paseadores y reseñas de mascotas
    [RoutePrefix("api/admin/moderation")]
    public class AdminModerationController : ControladorAdminBase
    {
        private readonly IServicioModeracion servicioModeracion;

        public AdminModerationController(IServicioModeracion servicioModeracion,
                                         IRepositorioEntidadBase<User> repositorioUsuarios,
                                         IRepositorioEntidadBase<Error> repositorioErrores,
                                         IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioUsuarios, repositorioErrores, unidadDeTrabajo)
        {
            this.servicioModeracion = servicioModeracion;
        }

        [HttpGet]
        [Route("ratings")]
        public HttpResponseMessage Ratings(HttpRequestMessage pedido, String status = null, Int32? maxStars = null, String search = null, Int32 page = 1, Int32 pageSize = 15)
        {
            return ComoAdministrador(pedido, idAdministrador =>
                pedido.CreateResponse(HttpStatusCode.OK, servicioModeracion.ListarValoraciones(status, maxStars, search, page, pageSize)));
        }

        [HttpPost]
        [Route("ratings/hide")]
        public HttpResponseMessage HideRating(HttpRequestMessage pedido, HideReviewDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioModeracion.OcultarValoracion(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        [HttpPost]
        [Route("ratings/show")]
        public HttpResponseMessage ShowRating(HttpRequestMessage pedido, ShowReviewDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioModeracion.MostrarValoracion(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        [HttpGet]
        [Route("petReviews")]
        public HttpResponseMessage PetReviews(HttpRequestMessage pedido, String status = null, Int32? maxStars = null, String search = null, Int32 page = 1, Int32 pageSize = 15)
        {
            return ComoAdministrador(pedido, idAdministrador =>
                pedido.CreateResponse(HttpStatusCode.OK, servicioModeracion.ListarResenasMascotas(status, maxStars, search, page, pageSize)));
        }

        [HttpPost]
        [Route("petReviews/hide")]
        public HttpResponseMessage HidePetReview(HttpRequestMessage pedido, HideReviewDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioModeracion.OcultarResenaMascota(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        [HttpPost]
        [Route("petReviews/show")]
        public HttpResponseMessage ShowPetReview(HttpRequestMessage pedido, ShowReviewDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioModeracion.MostrarResenaMascota(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }
    }
}
