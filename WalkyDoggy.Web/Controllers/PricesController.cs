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
    [RoutePrefix("api/prices")]
    public class PricesController : ControladorApiBase
    {
        private readonly IRepositorioEntidadBase<Price> repositorioPrecios;
        private readonly IServicioMembresia servicioMembresia;
        private readonly IServicioPrecios servicioPrecios;

        public PricesController(IRepositorioEntidadBase<Price> repositorioPrecios,
                                 IServicioMembresia servicioMembresia,
                                 IServicioPrecios servicioPrecios,
                                 IRepositorioEntidadBase<Error> repositorioErrores,
                                 IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.repositorioPrecios = repositorioPrecios;
            this.servicioMembresia = servicioMembresia;
            this.servicioPrecios = servicioPrecios;
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage pedido)
        {
            return CrearRespuestaHttp(pedido, () =>
            {

                HttpResponseMessage respuesta = null;
                var preciosDto = this.servicioPrecios.ObtenerTodos();

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, preciosDto);

                return respuesta;
            });
        }
    }
}