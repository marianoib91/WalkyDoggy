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
    //Chat de una reserva entre el paseador y el cliente. Solo lo puede leer y escribir quien inicio sesion como uno de los dos.
    [RoutePrefix("api/messages")]
    public class MessagesController : ControladorApiBase
    {
        private readonly IServicioMensajes servicioMensajes;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;

        public MessagesController(IServicioMensajes servicioMensajes,
                                  IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                  IRepositorioEntidadBase<Customer> repositorioClientes,
                                  IRepositorioEntidadBase<Error> repositorioErrores,
                                  IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.servicioMensajes = servicioMensajes;
            this.repositorioPaseadores = repositorioPaseadores;
            this.repositorioClientes = repositorioClientes;
        }

        private Boolean VerificarActor(HttpRequestMessage pedido, String actor, Int64 idActor, out HttpResponseMessage denegado)
        {
            denegado = null;

            if (!IdentidadDelActor.EstaAutenticado(User))
            {
                denegado = pedido.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
                return false;
            }

            var permitido = actor == "Walker" ? IdentidadDelActor.EsPaseador(User, repositorioPaseadores, idActor)
                        : actor == "Customer" ? IdentidadDelActor.EsCliente(User, repositorioClientes, idActor)
                        : false;
            if (!permitido)
            {
                denegado = pedido.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés ver ni escribir en el chat de otra persona." });
                return false;
            }

            return true;
        }

        //Mensajes de la reserva. after: solo los que tienen id mayor (para pedir los nuevos). Lo que escribio la otra persona queda como leido.
        [HttpGet]
        [Route("list")]
        public HttpResponseMessage List(HttpRequestMessage pedido, String bookingKey, String actor, Int64 actorId, Int64 after = 0)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage denegado;
                if (!VerificarActor(pedido, actor, actorId, out denegado))
                {
                    return denegado;
                }

                String error;
                var chat = this.servicioMensajes.ObtenerChat(bookingKey, actor, actorId, after, out error);
                if (chat == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return pedido.CreateResponse(HttpStatusCode.OK, chat);
            });
        }

        [HttpPost]
        [Route("send")]
        public HttpResponseMessage Send(HttpRequestMessage pedido, SendMessageDto mensajeDto)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                if (mensajeDto == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "Faltan datos del mensaje." });
                }

                HttpResponseMessage denegado;
                if (!VerificarActor(pedido, mensajeDto.Actor, mensajeDto.ActorId, out denegado))
                {
                    return denegado;
                }

                String error;
                var mensaje = this.servicioMensajes.Enviar(mensajeDto, out error);
                if (mensaje == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return pedido.CreateResponse(HttpStatusCode.OK, mensaje);
            });
        }
    }
}
