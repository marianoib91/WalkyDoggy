using System.Net;
using System.Net.Http;
using System.Web.Http;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    //Las caracteristicas que se ofrecen al cargar una mascota (solo las activas). Es un dato publico, como las razas.
    [RoutePrefix("api/traits")]
    public class TraitsController : ControladorApiBase
    {
        private readonly IServicioCaracteristicas servicioCaracteristicas;

        public TraitsController(IServicioCaracteristicas servicioCaracteristicas,
                                IRepositorioEntidadBase<Error> repositorioErrores,
                                IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.servicioCaracteristicas = servicioCaracteristicas;
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage pedido)
        {
            return CrearRespuestaHttp(pedido, () => pedido.CreateResponse(HttpStatusCode.OK, servicioCaracteristicas.ObtenerPares()));
        }
    }
}
