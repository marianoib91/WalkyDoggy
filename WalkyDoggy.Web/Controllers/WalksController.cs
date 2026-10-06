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
    [RoutePrefix("api/walks")]
    public class WalksController : ControladorApiBase
    {
        private readonly IServicioPaseos servicioPaseos;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;

        public WalksController(IServicioPaseos servicioPaseos,
                                 IRepositorioEntidadBase<Customer> repositorioClientes,
                                 IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                 IRepositorioEntidadBase<Error> repositorioErrores,
                                 IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.servicioPaseos = servicioPaseos;
            this.repositorioClientes = repositorioClientes;
            this.repositorioPaseadores = repositorioPaseadores;
        }

        //Confirmar o cancelar una reserva (cancelar puede devolver un pago) solo lo puede hacer quien inicio sesion como ese paseador o cliente
        private Boolean VerificarActor(HttpRequestMessage pedido, BookingActionCriteria accion, out HttpResponseMessage denegado)
        {
            denegado = null;

            if (accion == null)
            {
                denegado = pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "Faltan datos de la reserva." });
                return false;
            }
            if (!IdentidadDelActor.EstaAutenticado(User))
            {
                denegado = pedido.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
                return false;
            }

            var permitido = accion.Actor == "Walker" ? IdentidadDelActor.EsPaseador(User, repositorioPaseadores, accion.ActorId)
                        : accion.Actor == "Customer" ? IdentidadDelActor.EsCliente(User, repositorioClientes, accion.ActorId)
                        : false;
            if (!permitido)
            {
                denegado = pedido.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés operar sobre la reserva de otra persona." });
                return false;
            }

            return true;
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage pedido)
        {
            return CrearRespuestaHttp(pedido, () =>
            {

                HttpResponseMessage respuesta = null;

                var paseosDto = this.servicioPaseos.ObtenerTodos();

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, paseosDto);

                return respuesta;
            });
        }

        [HttpGet]
        [Route("getAllByWalkerId")]
        public HttpResponseMessage GetAllByWalkerId(HttpRequestMessage pedido, Int64 walkerId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {

                HttpResponseMessage respuesta = null;

                var paseosDto = this.servicioPaseos.ObtenerTodosPorIdPaseador(walkerId);

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, paseosDto);

                return respuesta;
            });
        }

        [HttpGet]
        [Route("getAllForCurrentDay")]
        public HttpResponseMessage GetAllForCurrentDay(HttpRequestMessage pedido, Int64 walkerId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {

                HttpResponseMessage respuesta = null;

                var paseosDto = this.servicioPaseos.ObtenerTodosDelDiaActual(walkerId);

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, paseosDto);

                return respuesta;
            });
        }

        [HttpGet]
        [Route("getBookingsForWalker")]
        public HttpResponseMessage GetBookingsForWalker(HttpRequestMessage pedido, Int64 walkerId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                var reservasDto = this.servicioPaseos.ObtenerReservasDelPaseador(walkerId);

                return pedido.CreateResponse(HttpStatusCode.OK, reservasDto);
            });
        }

        [HttpGet]
        [Route("getBookingsForCustomer")]
        public HttpResponseMessage GetBookingsForCustomer(HttpRequestMessage pedido, Int64 customerId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                var reservasDto = this.servicioPaseos.ObtenerReservasDelCliente(customerId);

                return pedido.CreateResponse(HttpStatusCode.OK, reservasDto);
            });
        }

        [HttpPost]
        [Route("confirm")]
        public HttpResponseMessage Confirm(HttpRequestMessage pedido, BookingActionCriteria criterioAccionReserva)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage denegado;
                if (!VerificarActor(pedido, criterioAccionReserva, out denegado))
                {
                    return denegado;
                }

                String error;
                if (!this.servicioPaseos.Confirmar(criterioAccionReserva, out error))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return pedido.CreateResponse(HttpStatusCode.OK, true);
            });
        }

        //El paseo lo inicia el cliente cuando el paseador llega a buscar a la mascota (o el paseador, pasados 15 minutos de la hora agendada): queda registrado el horario real
        [HttpPost]
        [Route("start")]
        public HttpResponseMessage Start(HttpRequestMessage pedido, BookingActionCriteria criterioAccionReserva)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage denegado;
                if (!VerificarActor(pedido, criterioAccionReserva, out denegado))
                {
                    return denegado;
                }

                String error;
                if (!this.servicioPaseos.Iniciar(criterioAccionReserva, out error))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return pedido.CreateResponse(HttpStatusCode.OK, true);
            });
        }

        //El paseador da por finalizado el paseo (ya devolvio a la mascota): desde ahi el cliente puede pagarlo
        [HttpPost]
        [Route("finish")]
        public HttpResponseMessage Finish(HttpRequestMessage pedido, BookingActionCriteria criterioAccionReserva)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage denegado;
                if (!VerificarActor(pedido, criterioAccionReserva, out denegado))
                {
                    return denegado;
                }

                String error = null;
                if (criterioAccionReserva.Actor != "Walker" || !this.servicioPaseos.Finalizar(criterioAccionReserva, out error))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { criterioAccionReserva.Actor != "Walker" ? "Solo el paseador puede dar el paseo por finalizado." : error });
                }

                return pedido.CreateResponse(HttpStatusCode.OK, true);
            });
        }

        //El paseador confirma que recibio el pago (en efectivo o con Mercado Pago) y la reserva queda cerrada
        [HttpPost]
        [Route("receive")]
        public HttpResponseMessage Receive(HttpRequestMessage pedido, BookingActionCriteria criterioAccionReserva)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage denegado;
                if (!VerificarActor(pedido, criterioAccionReserva, out denegado))
                {
                    return denegado;
                }

                String error = null;
                if (criterioAccionReserva.Actor != "Walker" || !this.servicioPaseos.ConfirmarRecibido(criterioAccionReserva, out error))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { criterioAccionReserva.Actor != "Walker" ? "Solo el paseador puede confirmar que recibió el pago." : error });
                }

                return pedido.CreateResponse(HttpStatusCode.OK, true);
            });
        }

        [HttpPost]
        [Route("cancel")]
        public HttpResponseMessage Cancel(HttpRequestMessage pedido, BookingActionCriteria criterioAccionReserva)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage denegado;
                if (!VerificarActor(pedido, criterioAccionReserva, out denegado))
                {
                    return denegado;
                }

                String error;
                if (!this.servicioPaseos.Cancelar(criterioAccionReserva, out error))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return pedido.CreateResponse(HttpStatusCode.OK, true);
            });
        }

        [HttpPost]
        [Route("register")]
        public HttpResponseMessage Register(HttpRequestMessage pedido, WalkRequestCriteria criterioSolicitudPaseo)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                String error;
                var paseosDto = this.servicioPaseos.Registrar(criterioSolicitudPaseo, out error);
                if (paseosDto == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return pedido.CreateResponse(HttpStatusCode.OK, paseosDto);
            });
        }

        [HttpPost]
        [Route("validatePetsInWalks")]
        public HttpResponseMessage ValidatePetsInWalks(HttpRequestMessage pedido, AvailableWalkersCriteria criterioPaseadoresDisponibles)
        {
            return CrearRespuestaHttp(pedido, () =>
            {

                HttpResponseMessage respuesta = null;

                var paseoDto = this.servicioPaseos.ValidarMascotasEnPaseos(criterioPaseadoresDisponibles);

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, paseoDto);

                return respuesta;
            });
        }
    }
}