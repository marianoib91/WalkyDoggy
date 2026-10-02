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
    [RoutePrefix("api/provinces")]
    public class ProvincesController : ControladorApiBase
    {
        private readonly IRepositorioEntidadBase<Province> repositorioProvincias;
        private readonly IServicioMembresia servicioMembresia;
        private readonly IServicioProvincias servicioProvincias;

        public ProvincesController(IRepositorioEntidadBase<Province> repositorioProvincias,
                                 IServicioMembresia servicioMembresia,
                                 IServicioProvincias servicioProvincias,
                                 IRepositorioEntidadBase<Error> repositorioErrores,
                                 IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.repositorioProvincias = repositorioProvincias;
            this.servicioMembresia = servicioMembresia;
            this.servicioProvincias = servicioProvincias;
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage pedido)
        {
            return CrearRespuestaHttp(pedido, () =>
            {

                HttpResponseMessage respuesta = null;
                var provinciasDto = this.servicioProvincias.ObtenerTodos();

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, provinciasDto);

                return respuesta;
            });
        }
    }
}