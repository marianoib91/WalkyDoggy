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
        private readonly IRepositorioEntidadBase<Stay> repositorioHospedajes;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;

        public ServicioMensajes(IRepositorioEntidadBase<Message> repositorioMensajes,
                                IRepositorioEntidadBase<Walk> repositorioPaseos,
                                IRepositorioEntidadBase<Stay> repositorioHospedajes,
                                IUnidadDeTrabajo unidadDeTrabajo)
        {
            this.repositorioMensajes = repositorioMensajes;
            this.repositorioPaseos = repositorioPaseos;
            this.repositorioHospedajes = repositorioHospedajes;
            this.unidadDeTrabajo = unidadDeTrabajo;
        }

        //Las claves de hospedaje son "h" + id (las de los paseos son "w" + id o un codigo hexadecimal, asi que no se pisan)
        private static Boolean EsClaveDeHospedaje(String clave, out Int64 id)
        {
            id = 0;
            return !String.IsNullOrEmpty(clave) && clave.StartsWith("h") && Int64.TryParse(clave.Substring(1), out id);
        }

        //El hospedaje de la clave si quien pregunta es su cuidador o su cliente; si no, null
        private Stay BuscarHospedajeDelActor(Int64 id, String actor, Int64 idActor)
        {
            var hospedaje = this.repositorioHospedajes.TodosConIncluidos(x => x.Walker, x => x.Customer).FirstOrDefault(x => x.Id == id);
            if (hospedaje == null)
            {
                return null;
            }

            if (actor == RolPaseador && hospedaje.WalkerId == idActor) { return hospedaje; }
            if (actor == RolCliente && hospedaje.CustomerId == idActor) { return hospedaje; }
            return null;
        }

        //Se puede escribir mientras el hospedaje este confirmado y todavia no se haya cobrado
        private static Boolean SePuedeEscribirEnHospedaje(Stay hospedaje)
        {
            return hospedaje.Status == WalkStatus.Confirmed && !hospedaje.ReceivedAt.HasValue;
        }

        public ChatDto ObtenerChat(String claveReserva, String actor, Int64 idActor, Int64 despuesDeId, out String error)
        {
            error = null;

            Int64 idHospedaje;
            var esHospedaje = EsClaveDeHospedaje(claveReserva, out idHospedaje);
            var hospedaje = esHospedaje ? BuscarHospedajeDelActor(idHospedaje, actor, idActor) : null;
            var paseos = esHospedaje ? new List<Walk>() : BuscarReservaDelActor(claveReserva, actor, idActor);
            if (esHospedaje ? hospedaje == null : paseos == null)
            {
                error = esHospedaje ? "El hospedaje no existe." : "La reserva no existe.";
                return null;
            }

            var mensajes = this.repositorioMensajes.ObtenerTodos().
                                              Where(x => x.BookingKey == claveReserva).
                                              OrderBy(x => x.Id).
                                              ToList();

            var pendiente = esHospedaje ? hospedaje.Status == WalkStatus.Pending : paseos.All(x => x.Status == WalkStatus.Pending);
            if (mensajes.Count == 0 && pendiente)
            {
                error = esHospedaje ? "El chat se habilita cuando el cuidador confirma el hospedaje." : "El chat se habilita cuando el paseador confirma la reserva.";
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
                CanWrite = esHospedaje ? SePuedeEscribirEnHospedaje(hospedaje) : SePuedeEscribir(paseos),
                Walker = esHospedaje ? Participante(hospedaje.Walker == null ? null : hospedaje.Walker.FirstName, hospedaje.Walker == null ? null : hospedaje.Walker.LastName, hospedaje.Walker == null ? null : hospedaje.Walker.ProfileImage)
                                     : Participante(paseos[0].Walker == null ? null : paseos[0].Walker.FirstName, paseos[0].Walker == null ? null : paseos[0].Walker.LastName, paseos[0].Walker == null ? null : paseos[0].Walker.ProfileImage),
                Customer = esHospedaje ? Participante(hospedaje.Customer == null ? null : hospedaje.Customer.FirstName, hospedaje.Customer == null ? null : hospedaje.Customer.LastName, hospedaje.Customer == null ? null : hospedaje.Customer.ProfileImage)
                                       : Participante(paseos[0].Pet.Customer == null ? null : paseos[0].Pet.Customer.FirstName, paseos[0].Pet.Customer == null ? null : paseos[0].Pet.Customer.LastName, paseos[0].Pet.Customer == null ? null : paseos[0].Pet.Customer.ProfileImage)
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

            Int64 idHospedaje;
            var esHospedaje = EsClaveDeHospedaje(solicitud.BookingKey, out idHospedaje);
            var hospedaje = esHospedaje ? BuscarHospedajeDelActor(idHospedaje, solicitud.Actor, solicitud.ActorId) : null;
            var paseos = esHospedaje ? new List<Walk>() : BuscarReservaDelActor(solicitud.BookingKey, solicitud.Actor, solicitud.ActorId);
            if (esHospedaje ? hospedaje == null : paseos == null)
            {
                error = esHospedaje ? "El hospedaje no existe." : "La reserva no existe.";
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

            if (esHospedaje ? hospedaje.Status == WalkStatus.Pending : paseos.All(x => x.Status == WalkStatus.Pending))
            {
                error = esHospedaje ? "El chat se habilita cuando el cuidador confirma el hospedaje." : "El chat se habilita cuando el paseador confirma la reserva.";
                return null;
            }
            if (esHospedaje ? !SePuedeEscribirEnHospedaje(hospedaje) : !SePuedeEscribir(paseos))
            {
                error = esHospedaje ? "El chat de este hospedaje está cerrado." : "El chat de esta reserva está cerrado.";
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
            var paseos = AyudanteReservas.Buscar(this.repositorioPaseos.TodosConIncluidos(x => x.Pet, x => x.Pet.Customer, x => x.Walker), claveReserva);
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

        private static ChatParticipantDto Participante(String nombre, String apellido, String foto)
        {
            return new ChatParticipantDto
            {
                Name = ((nombre ?? String.Empty) + " " + (apellido ?? String.Empty)).Trim(),
                ProfileImage = foto
            };
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
