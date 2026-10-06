using System;
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
    //Sugerencias discretas para el cliente (nada de mails): solo las ve quien inicio sesion como ese cliente
    [RoutePrefix("api/predictions")]
    public class PredictionsController : ControladorApiBase
    {
        private readonly IServicioPrediccion servicioPrediccion;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;

        public PredictionsController(IServicioPrediccion servicioPrediccion,
                                     IRepositorioEntidadBase<Customer> repositorioClientes,
                                     IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                     IRepositorioEntidadBase<Error> repositorioErrores,
                                     IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.servicioPrediccion = servicioPrediccion;
            this.repositorioClientes = repositorioClientes;
            this.repositorioPaseadores = repositorioPaseadores;
        }

        //Los dias en que el paseador suele tener mas reservas (204 si todavia tiene pocas)
        [HttpGet]
        [Route("walkerDemand")]
        public HttpResponseMessage WalkerDemand(HttpRequestMessage pedido, Int64 walkerId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                if (!IdentidadDelActor.EstaAutenticado(User))
                {
                    return pedido.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
                }
                if (!IdentidadDelActor.EsPaseador(User, repositorioPaseadores, walkerId))
                {
                    return pedido.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés ver la demanda de otra persona." });
                }

                var demanda = servicioPrediccion.DemandaDelPaseador(walkerId, DateTime.Now);
                return demanda == null
                    ? pedido.CreateResponse(HttpStatusCode.NoContent)
                    : pedido.CreateResponse(HttpStatusCode.OK, demanda);
            });
        }

        //La sugerencia de reserva del cliente (204 si no hay un patron claro)
        [HttpGet]
        [Route("nextBooking")]
        public HttpResponseMessage NextBooking(HttpRequestMessage pedido, Int64 customerId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                if (!IdentidadDelActor.EstaAutenticado(User))
                {
                    return pedido.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
                }
                if (!IdentidadDelActor.EsCliente(User, repositorioClientes, customerId))
                {
                    return pedido.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés ver las sugerencias de otra persona." });
                }

                var sugerencia = servicioPrediccion.SugerirReserva(customerId, DateTime.Now);
                return sugerencia == null
                    ? pedido.CreateResponse(HttpStatusCode.NoContent)
                    : pedido.CreateResponse(HttpStatusCode.OK, sugerencia);
            });
        }
    }
}
