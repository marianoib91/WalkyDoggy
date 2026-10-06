using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WalkyDoggy.Application.Criterias;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    //Hospedaje: perros que pasan varias noches en la casa de un cuidador (un paseador que activa la opcion).
    //Todo exige sesion iniciada como el paseador o el cliente que corresponde.
    [RoutePrefix("api/stays")]
    public class StaysController : ControladorApiBase
    {
        private readonly IServicioHospedajes servicioHospedajes;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;

        public StaysController(IServicioHospedajes servicioHospedajes,
                               IRepositorioEntidadBase<Customer> repositorioClientes,
                               IRepositorioEntidadBase<Walker> repositorioPaseadores,
                               IRepositorioEntidadBase<Error> repositorioErrores,
                               IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.servicioHospedajes = servicioHospedajes;
            this.repositorioClientes = repositorioClientes;
            this.repositorioPaseadores = repositorioPaseadores;
        }

        private HttpResponseMessage Denegar(HttpRequestMessage pedido, String actor, Int64 idActor)
        {
            if (!IdentidadDelActor.EstaAutenticado(User))
            {
                return pedido.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
            }

            var permitido = actor == "Walker" ? IdentidadDelActor.EsPaseador(User, repositorioPaseadores, idActor)
                        : actor == "Customer" ? IdentidadDelActor.EsCliente(User, repositorioClientes, idActor)
                        : false;
            return permitido ? null : pedido.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés operar sobre el hospedaje de otra persona." });
        }

        //La oferta de hospedaje del paseador (solo la ve él)
        [HttpGet]
        [Route("offer")]
        public HttpResponseMessage GetOffer(HttpRequestMessage pedido, Int64 walkerId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                var denegado = Denegar(pedido, "Walker", walkerId);
                if (denegado != null)
                {
                    return denegado;
                }

                var oferta = servicioHospedajes.ObtenerOferta(walkerId);
                return oferta == null ? pedido.CreateResponse(HttpStatusCode.NotFound, new[] { "El paseador no existe." }) : pedido.CreateResponse(HttpStatusCode.OK, oferta);
            });
        }

        [HttpPost]
        [Route("saveOffer")]
        public HttpResponseMessage SaveOffer(HttpRequestMessage pedido, BoardingOfferDto oferta)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                if (oferta == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "Faltan datos de la oferta." });
                }
                var denegado = Denegar(pedido, "Walker", oferta.WalkerId);
                if (denegado != null)
                {
                    return denegado;
                }

                String error;
                return servicioHospedajes.GuardarOferta(oferta, out error)
                    ? pedido.CreateResponse(HttpStatusCode.OK, servicioHospedajes.ObtenerOferta(oferta.WalkerId))
                    : pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
            });
        }

        //Cuidadores con lugar para esas fechas y esa cantidad de perros (los ve cualquier cliente con sesion)
        [HttpGet]
        [Route("search")]
        public HttpResponseMessage Search(HttpRequestMessage pedido, Int64 customerId, String checkIn, String checkOut, Int32 dogs, String latitude = null, String longitude = null)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                var denegado = Denegar(pedido, "Customer", customerId);
                if (denegado != null)
                {
                    return denegado;
                }

                String error;
                var cuidadores = servicioHospedajes.Buscar(checkIn, checkOut, dogs, latitude, longitude, out error);
                return cuidadores == null
                    ? pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error })
                    : pedido.CreateResponse(HttpStatusCode.OK, cuidadores);
            });
        }

        [HttpPost]
        [Route("request")]
        public HttpResponseMessage Request(HttpRequestMessage pedido, StayRequestDto solicitud)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                if (solicitud == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "Faltan datos del hospedaje." });
                }
                var denegado = Denegar(pedido, "Customer", solicitud.CustomerId);
                if (denegado != null)
                {
                    return denegado;
                }

                String error;
                var hospedaje = servicioHospedajes.Solicitar(solicitud, out error);
                return hospedaje == null
                    ? pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error })
                    : pedido.CreateResponse(HttpStatusCode.OK, hospedaje);
            });
        }

        [HttpGet]
        [Route("getForWalker")]
        public HttpResponseMessage GetForWalker(HttpRequestMessage pedido, Int64 walkerId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                var denegado = Denegar(pedido, "Walker", walkerId);
                return denegado ?? pedido.CreateResponse(HttpStatusCode.OK, servicioHospedajes.ObtenerDelPaseador(walkerId));
            });
        }

        [HttpGet]
        [Route("getForCustomer")]
        public HttpResponseMessage GetForCustomer(HttpRequestMessage pedido, Int64 customerId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                var denegado = Denegar(pedido, "Customer", customerId);
                return denegado ?? pedido.CreateResponse(HttpStatusCode.OK, servicioHospedajes.ObtenerDelCliente(customerId));
            });
        }

        private delegate Boolean AccionDeHospedaje(BookingActionCriteria accion, out String error);

        //Comprueba que quien actua es el paseador o el cliente que dice ser y ejecuta la accion
        private HttpResponseMessage Ejecutar(HttpRequestMessage pedido, BookingActionCriteria accion, AccionDeHospedaje operacion)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                if (accion == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "Faltan datos del hospedaje." });
                }
                var denegado = Denegar(pedido, accion.Actor, accion.ActorId);
                if (denegado != null)
                {
                    return denegado;
                }

                String error;
                return operacion(accion, out error)
                    ? pedido.CreateResponse(HttpStatusCode.OK, new { ok = true })
                    : pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error ?? "No se pudo completar la acción." });
            });
        }

        [HttpPost]
        [Route("confirm")]
        public HttpResponseMessage Confirm(HttpRequestMessage pedido, BookingActionCriteria accion)
        {
            return Ejecutar(pedido, accion, servicioHospedajes.Confirmar);
        }

        [HttpPost]
        [Route("cancel")]
        public HttpResponseMessage Cancel(HttpRequestMessage pedido, BookingActionCriteria accion)
        {
            return Ejecutar(pedido, accion, servicioHospedajes.Cancelar);
        }

        [HttpPost]
        [Route("start")]
        public HttpResponseMessage Start(HttpRequestMessage pedido, BookingActionCriteria accion)
        {
            return Ejecutar(pedido, accion, servicioHospedajes.Iniciar);
        }

        [HttpPost]
        [Route("finish")]
        public HttpResponseMessage Finish(HttpRequestMessage pedido, BookingActionCriteria accion)
        {
            return Ejecutar(pedido, accion, servicioHospedajes.Finalizar);
        }

        [HttpPost]
        [Route("receive")]
        public HttpResponseMessage Receive(HttpRequestMessage pedido, BookingActionCriteria accion)
        {
            return Ejecutar(pedido, accion, servicioHospedajes.ConfirmarRecibido);
        }
    }
}
