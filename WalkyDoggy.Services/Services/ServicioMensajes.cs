using System;
using System.Collections.Generic;
using System.Linq;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Services.Utilities;

namespace WalkyDoggy.Services.Services
{
    public class ServicioMensajes : IServicioMensajes
    {
        public const Int32 LargoMaximoMensaje = 500;

        private const String RolPaseador = "Walker";
        private const String RolCliente = "Customer";
        private const String RolSistema = "System";

        private readonly IRepositorioEntidadBase<Message> repositorioMensajes;
        private readonly IRepositorioEntidadBase<Walk> repositorioPaseos;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;

        public ServicioMensajes(IRepositorioEntidadBase<Message> repositorioMensajes,
                                IRepositorioEntidadBase<Walk> repositorioPaseos,
                                IUnidadDeTrabajo unidadDeTrabajo)
        {
            this.repositorioMensajes = repositorioMensajes;
            this.repositorioPaseos = repositorioPaseos;
            this.unidadDeTrabajo = unidadDeTrabajo;
        }

        public ChatDto ObtenerChat(String claveReserva, String actor, Int64 idActor, Int64 despuesDeId, out String error)
        {
            error = null;

            var paseos = BuscarReservaDelActor(claveReserva, actor, idActor);
            if (paseos == null)
            {
                error = "La reserva no existe.";
                return null;
            }

            var mensajes = this.repositorioMensajes.ObtenerTodos().
                                              Where(x => x.BookingKey == claveReserva).
                                              OrderBy(x => x.Id).
                                              ToList();

            if (mensajes.Count == 0 && paseos.All(x => x.Status == WalkStatus.Pending))
            {
                error = "El chat se habilita cuando el paseador confirma la reserva.";
                return null;
            }

            //Lo que escribio la otra persona queda como leido
            var sinLeer = mensajes.Where(x => x.ReadAt == null && x.SenderRole != actor && x.SenderRole != RolSistema).ToList();
            if (sinLeer.Count > 0)
            {
                var ahora = DateTime.Now;
                sinLeer.ForEach(x => x.ReadAt = ahora);
                this.unidadDeTrabajo.GuardarCambios();
            }

            return new ChatDto
            {
                Messages = mensajes.Where(x => x.Id > despuesDeId).Select(x => ADto(x)).ToList(),
                CanWrite = SePuedeEscribir(paseos)
            };
        }

        public MessageDto Enviar(SendMessageDto solicitud, out String error)
        {
            error = null;

            if (solicitud == null)
            {
                error = "Faltan datos del mensaje.";
                return null;
            }

            var paseos = BuscarReservaDelActor(solicitud.BookingKey, solicitud.Actor, solicitud.ActorId);
            if (paseos == null)
            {
                error = "La reserva no existe.";
                return null;
            }

            var texto = (solicitud.Text ?? String.Empty).Trim();
            if (texto.Length == 0)
            {
                error = "Escribí un mensaje.";
                return null;
            }
            if (texto.Length > LargoMaximoMensaje)
            {
                error = "El mensaje puede tener hasta " + LargoMaximoMensaje + " caracteres.";
                return null;
            }

            if (paseos.All(x => x.Status == WalkStatus.Pending))
            {
                error = "El chat se habilita cuando el paseador confirma la reserva.";
                return null;
            }
            if (!SePuedeEscribir(paseos))
            {
                error = "El chat de esta reserva está cerrado.";
                return null;
            }

            var mensaje = new Message
            {
                BookingKey = solicitud.BookingKey,
                SenderRole = solicitud.Actor,
                SenderId = solicitud.ActorId,
                Text = texto,
                SentAt = DateTime.Now
            };
            this.repositorioMensajes.Agregar(mensaje);
            this.unidadDeTrabajo.GuardarCambios();

            return ADto(mensaje);
        }

        public void AgregarDelSistema(String claveReserva, String texto)
        {
            this.repositorioMensajes.Agregar(new Message
            {
                BookingKey = claveReserva,
                SenderRole = RolSistema,
                Text = texto.Length > LargoMaximoMensaje ? texto.Substring(0, LargoMaximoMensaje) : texto,
                SentAt = DateTime.Now
            });
            this.unidadDeTrabajo.GuardarCambios();
        }

        public Dictionary<String, ResumenDeChat> ObtenerResumenes(IEnumerable<String> clavesReserva, String rol)
        {
            var claves = clavesReserva.Distinct().ToList();
            if (claves.Count == 0)
            {
                return new Dictionary<String, ResumenDeChat>();
            }

            var mensajes = this.repositorioMensajes.ObtenerTodos().
                                              Where(x => claves.Contains(x.BookingKey) && x.SenderRole != RolSistema).
                                              Select(x => new { x.BookingKey, x.SenderRole, x.ReadAt }).
                                              ToList();

            return mensajes.GroupBy(x => x.BookingKey).ToDictionary(
                g => g.Key,
                g => new ResumenDeChat
                {
                    Total = g.Count(),
                    NoLeidos = g.Count(x => x.SenderRole != rol && x.ReadAt == null)
                });
        }

        //Se puede escribir mientras la reserva este confirmada y todavia no se haya cobrado (cerrada)
        private static Boolean SePuedeEscribir(List<Walk> paseos)
        {
            return paseos.All(x => x.Status == WalkStatus.Confirmed) && paseos.All(x => !x.ReceivedAt.HasValue);
        }

        //Los paseos de la reserva, si quien pregunta es el paseador o el cliente de esa reserva; si no, null
        private List<Walk> BuscarReservaDelActor(String claveReserva, String actor, Int64 idActor)
        {
            var paseos = AyudanteReservas.Buscar(this.repositorioPaseos.TodosConIncluidos(x => x.Pet), claveReserva);
            if (paseos.Count == 0)
            {
                return null;
            }

            if (actor == RolPaseador && paseos[0].WalkerId == idActor)
            {
                return paseos;
            }
            if (actor == RolCliente && paseos[0].Pet != null && paseos[0].Pet.CustomerId == idActor)
            {
                return paseos;
            }

            return null;
        }

        private static MessageDto ADto(Message mensaje)
        {
            return new MessageDto
            {
                Id = mensaje.Id,
                SenderRole = mensaje.SenderRole,
                Text = mensaje.Text,
                SentAt = mensaje.SentAt,
                Read = mensaje.ReadAt.HasValue
            };
        }
    }
}
