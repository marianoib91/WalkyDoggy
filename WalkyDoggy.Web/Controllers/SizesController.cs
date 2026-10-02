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
    [RoutePrefix("api/sizes")]
    public class SizesController : ControladorApiBase
    {
        private readonly IRepositorioEntidadBase<Size> repositorioTamanos;
        private readonly IServicioMembresia servicioMembresia;
        private readonly IServicioTamanos servicioTamanos;

        public SizesController(IRepositorioEntidadBase<Size> repositorioTamanos,
                                 IServicioMembresia servicioMembresia,
                                 IServicioTamanos servicioTamanos,
                                 IRepositorioEntidadBase<Error> repositorioErrores,
                                 IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.repositorioTamanos = repositorioTamanos;
            this.servicioMembresia = servicioMembresia;
            this.servicioTamanos = servicioTamanos;
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage pedido)
        {
            return CrearRespuestaHttp(pedido, () =>
            {

                HttpResponseMessage respuesta = null;
                var tamanosDto = this.servicioTamanos.ObtenerTodos();

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, tamanosDto);

                return respuesta;
            });
        }
    }
}