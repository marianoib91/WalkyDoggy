using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Application.Criterias;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/cities")]
    public class CitiesController : ControladorApiBase
    {
        private readonly IRepositorioEntidadBase<City> repositorioCiudades;
        private readonly IServicioMembresia servicioMembresia;
        private readonly IServicioCiudades servicioCiudades;

        public CitiesController(IRepositorioEntidadBase<City> repositorioCiudades,
                                 IServicioMembresia servicioMembresia,
                                 IServicioCiudades servicioCiudades,
                                 IRepositorioEntidadBase<Error> repositorioErrores,
                                 IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.repositorioCiudades = repositorioCiudades;
            this.servicioMembresia = servicioMembresia;
            this.servicioCiudades = servicioCiudades;
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage pedido)
        {
            return CrearRespuestaHttp(pedido, () =>
            {

                HttpResponseMessage respuesta = null;
                var ciudadesDto = this.servicioCiudades.ObtenerTodos();

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, ciudadesDto);

                return respuesta;
            });
        }

        [HttpPost]
        [Route("resolve")]
        public HttpResponseMessage Resolve(HttpRequestMessage pedido, CityResolveCriteria criterioResolverCiudad)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                var ciudadDto = this.servicioCiudades.Resolver(criterioResolverCiudad);
                if (ciudadDto == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "No se pudo reconocer la ciudad o la provincia de la dirección." });
                }

                return pedido.CreateResponse(HttpStatusCode.OK, ciudadDto);
            });
        }

        [HttpGet]
        [Route("getAllByProvinceId")]
        public HttpResponseMessage GetAllByProvinceId(HttpRequestMessage pedido, Int64 provinceId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {

                HttpResponseMessage respuesta = null;
                var ciudadesDto = this.servicioCiudades.ObtenerTodosPorIdProvincia(provinceId);

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, ciudadesDto);

                return respuesta;
            });
        }
    }
}