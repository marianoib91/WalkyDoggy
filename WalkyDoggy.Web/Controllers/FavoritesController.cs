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
    //Paseadores favoritos de un cliente. Solo los puede ver y cambiar quien inicio sesion como ese cliente.
    [RoutePrefix("api/favorites")]
    public class FavoritesController : ControladorApiBase
    {
        private readonly IServicioFavoritos servicioFavoritos;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;

        public FavoritesController(IServicioFavoritos servicioFavoritos,
                                   IRepositorioEntidadBase<Customer> repositorioClientes,
                                   IRepositorioEntidadBase<Error> repositorioErrores,
                                   IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.servicioFavoritos = servicioFavoritos;
            this.repositorioClientes = repositorioClientes;
        }

        private Boolean VerificarCliente(HttpRequestMessage pedido, Int64 idCliente, out HttpResponseMessage denegado)
        {
            denegado = null;

            if (!IdentidadDelActor.EstaAutenticado(User))
            {
                denegado = pedido.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
                return false;
            }
            if (!IdentidadDelActor.EsCliente(User, repositorioClientes, idCliente))
            {
                denegado = pedido.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés ver ni cambiar los favoritos de otra persona." });
                return false;
            }

            return true;
        }

        //Ids de los paseadores favoritos del cliente
        [HttpGet]
        [Route("list")]
        public HttpResponseMessage List(HttpRequestMessage pedido, Int64 customerId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage denegado;
                if (!VerificarCliente(pedido, customerId, out denegado))
                {
                    return denegado;
                }

                return pedido.CreateResponse(HttpStatusCode.OK, this.servicioFavoritos.ObtenerIdsPaseadores(customerId));
            });
        }

        [HttpPost]
        [Route("set")]
        public HttpResponseMessage Set(HttpRequestMessage pedido, SetFavoriteDto solicitud)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                if (solicitud == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "Faltan datos del paseador." });
                }

                HttpResponseMessage denegado;
                if (!VerificarCliente(pedido, solicitud.CustomerId, out denegado))
                {
                    return denegado;
                }

                String error;
                if (!this.servicioFavoritos.Establecer(solicitud, out error))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return pedido.CreateResponse(HttpStatusCode.OK, solicitud.Favorite);
            });
        }
    }
}
