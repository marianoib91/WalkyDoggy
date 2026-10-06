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
    //Los avisos que ven los clientes y los paseadores en su pantalla de inicio, y el conteo de vistas y clics.
    //Es informacion publica (como los paseadores o las razas): no hace falta iniciar sesion.
    [RoutePrefix("api/ads")]
    public class AdsController : ControladorApiBase
    {
        private readonly IServicioPublicidad servicioPublicidad;

        public AdsController(IServicioPublicidad servicioPublicidad,
                             IRepositorioEntidadBase<Error> repositorioErrores,
                             IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.servicioPublicidad = servicioPublicidad;
        }

        //audience: Customers (por defecto) o Walkers. Con latitude y longitude se agregan los avisos por zona que alcanzan a esa ubicacion.
        [HttpGet]
        [Route("active")]
        public HttpResponseMessage Active(HttpRequestMessage pedido, String audience = null, Double? latitude = null, Double? longitude = null, Int32 max = 3)
        {
            return CrearRespuestaHttp(pedido, () =>
                pedido.CreateResponse(HttpStatusCode.OK, servicioPublicidad.ObtenerActivos(audience, latitude, longitude, max)));
        }

        //Directorio "Comercios amigos": todos los comercios activos con la distancia a quien mira y sus promociones de hoy
        [HttpGet]
        [Route("friends")]
        public HttpResponseMessage Friends(HttpRequestMessage pedido, String audience = null, Double? latitude = null, Double? longitude = null)
        {
            return CrearRespuestaHttp(pedido, () =>
                pedido.CreateResponse(HttpStatusCode.OK, servicioPublicidad.ObtenerComerciosAmigos(audience, latitude, longitude)));
        }

        //Se avisa una vez cuando los avisos se muestran en pantalla
        [HttpPost]
        [Route("impressions")]
        public HttpResponseMessage Impressions(HttpRequestMessage pedido, AdImpressionsDto solicitud)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                servicioPublicidad.RegistrarVistas(solicitud == null ? null : solicitud.Ids);
                return pedido.CreateResponse(HttpStatusCode.OK, true);
            });
        }

        [HttpPost]
        [Route("click")]
        public HttpResponseMessage Click(HttpRequestMessage pedido, AdClickDto solicitud)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                var registrado = solicitud != null && servicioPublicidad.RegistrarClic(solicitud.Id);
                return pedido.CreateResponse(HttpStatusCode.OK, registrado);
            });
        }
    }
}
