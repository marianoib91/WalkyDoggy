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
    //Catalogos que administra el administrador: razas, tamaños y caracteristicas de las mascotas
    [RoutePrefix("api/admin/catalogs")]
    public class AdminCatalogsController : ControladorAdminBase
    {
        private readonly IServicioCatalogos servicioCatalogos;

        public AdminCatalogsController(IServicioCatalogos servicioCatalogos,
                                       IRepositorioEntidadBase<User> repositorioUsuarios,
                                       IRepositorioEntidadBase<Error> repositorioErrores,
                                       IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioUsuarios, repositorioErrores, unidadDeTrabajo)
        {
            this.servicioCatalogos = servicioCatalogos;
        }

        /* ---------- Razas ---------- */

        [HttpGet]
        [Route("breeds")]
        public HttpResponseMessage Breeds(HttpRequestMessage pedido)
        {
            return ComoAdministrador(pedido, idAdministrador => pedido.CreateResponse(HttpStatusCode.OK, servicioCatalogos.ListarRazas()));
        }

        [HttpPost]
        [Route("breeds/save")]
        public HttpResponseMessage SaveBreed(HttpRequestMessage pedido, SaveCatalogItemDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioCatalogos.GuardarRaza(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        [HttpPost]
        [Route("breeds/setActive")]
        public HttpResponseMessage SetBreedActive(HttpRequestMessage pedido, SetCatalogItemActiveDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioCatalogos.EstablecerRazaActiva(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        /* ---------- Tamaños ---------- */

        [HttpGet]
        [Route("sizes")]
        public HttpResponseMessage Sizes(HttpRequestMessage pedido)
        {
            return ComoAdministrador(pedido, idAdministrador => pedido.CreateResponse(HttpStatusCode.OK, servicioCatalogos.ListarTamanos()));
        }

        [HttpPost]
        [Route("sizes/save")]
        public HttpResponseMessage SaveSize(HttpRequestMessage pedido, SaveCatalogItemDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioCatalogos.GuardarTamano(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        [HttpPost]
        [Route("sizes/setActive")]
        public HttpResponseMessage SetSizeActive(HttpRequestMessage pedido, SetCatalogItemActiveDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioCatalogos.EstablecerTamanoActivo(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        /* ---------- Caracteristicas ---------- */

        [HttpGet]
        [Route("traits")]
        public HttpResponseMessage Traits(HttpRequestMessage pedido)
        {
            return ComoAdministrador(pedido, idAdministrador => pedido.CreateResponse(HttpStatusCode.OK, servicioCatalogos.ListarCaracteristicas()));
        }

        [HttpPost]
        [Route("traits/save")]
        public HttpResponseMessage SaveTraitPair(HttpRequestMessage pedido, SaveTraitPairDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioCatalogos.GuardarCaracteristicas(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        [HttpPost]
        [Route("traits/setActive")]
        public HttpResponseMessage SetTraitPairActive(HttpRequestMessage pedido, SetTraitPairActiveDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioCatalogos.EstablecerParActivo(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }
    }
}
