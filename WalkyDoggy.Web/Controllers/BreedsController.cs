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
    public class BreedsController : ControladorApiBase
    {
        private readonly IRepositorioEntidadBase<Breed> repositorioRazas;
        private readonly IServicioMembresia servicioMembresia;
        private readonly IServicioRazas servicioRazas;

        public BreedsController(IRepositorioEntidadBase<Breed> repositorioRazas,
                                 IServicioMembresia servicioMembresia,
                                 IServicioRazas servicioRazas,
                                 IRepositorioEntidadBase<Error> repositorioErrores,
                                 IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.repositorioRazas = repositorioRazas;
            this.servicioMembresia = servicioMembresia;
            this.servicioRazas = servicioRazas;
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage pedido)
        {
            return CrearRespuestaHttp(pedido, () =>
            {

                HttpResponseMessage respuesta = null;
                var razasDto = this.servicioRazas.ObtenerTodos();

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, razasDto);

                return respuesta;
            });
        }
    }
}