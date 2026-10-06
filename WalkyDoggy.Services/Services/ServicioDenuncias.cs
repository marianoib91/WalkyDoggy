using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class ServicioDenuncias : IServicioDenuncias
    {
        private const Int32 MinimoDescripcion = 10;
        private const Int32 MaximoDescripcion = 1000;
        private const Int32 MinimoNota = 5;
        private const Int32 MaximoNota = 500;
        private const Int32 MaximoImagenes = 5;
        private const Int32 MaximoPagina = 50;

        private static readonly CultureInfo CulturaEspanola = new CultureInfo("es-AR");

        private readonly IRepositorioEntidadBase<Complaint> repositorioDenuncias;
        private readonly IRepositorioEntidadBase<ComplaintImage> repositorioImagenes;
        private readonly IRepositorioEntidadBase<Walk> repositorioPaseos;
        private readonly IRepositorioEntidadBase<Stay> repositorioHospedajes;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;
        private readonly IRepositorioEntidadBase<User> repositorioUsuarios;
        private readonly IRepositorioEntidadBase<Message> repositorioMensajes;
        private readonly IRepositorioEntidadBase<AdminAction> repositorioAcciones;
        private readonly IServicioAdministracion servicioAdministracion;
        private readonly IServicioNotificaciones servicioNotificaciones;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;

        public ServicioDenuncias(IRepositorioEntidadBase<Complaint> repositorioDenuncias,
                                 IRepositorioEntidadBase<ComplaintImage> repositorioImagenes,
                                 IRepositorioEntidadBase<Walk> repositorioPaseos,
                                 IRepositorioEntidadBase<Stay> repositorioHospedajes,
                                 IRepositorioEntidadBase<Customer> repositorioClientes,
                                 IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                 IRepositorioEntidadBase<User> repositorioUsuarios,
                                 IRepositorioEntidadBase<Message> repositorioMensajes,
                                 IRepositorioEntidadBase<AdminAction> repositorioAcciones,
                                 IServicioAdministracion servicioAdministracion,
                                 IServicioNotificaciones servicioNotificaciones,
                                 IUnidadDeTrabajo unidadDeTrabajo)
        {
            this.repositorioDenuncias = repositorioDenuncias;
            this.repositorioImagenes = repositorioImagenes;
            this.repositorioPaseos = repositorioPaseos;
            this.repositorioHospedajes = repositorioHospedajes;
            this.repositorioClientes = repositorioClientes;
            this.repositorioPaseadores = repositorioPaseadores;
            this.repositorioUsuarios = repositorioUsuarios;
            this.repositorioMensajes = repositorioMensajes;
            this.repositorioAcciones = repositorioAcciones;
            this.servicioAdministracion = servicioAdministracion;
            this.servicioNotificaciones = servicioNotificaciones;
            this.unidadDeTrabajo = unidadDeTrabajo;
        }

        /* ---------- Quien denuncia ---------- */

        public Boolean Crear(CreateComplaintDto solicitud, List<ComplaintImageInfo> imagenes, out String error)
        {
            error = null;
            imagenes = imagenes ?? new List<ComplaintImageInfo>();

            if (solicitud == null || String.IsNullOrWhiteSpace(solicitud.BookingKey) ||
                (solicitud.Actor != WalkCancelledBy.Customer && solicitud.Actor != WalkCancelledBy.Walker))
            {
                error = "Faltan datos de la denuncia.";
                return false;
            }
            if (!ComplaintReasons.Todos.Contains(solicitud.Reason ?? String.Empty))
            {
                error = "Elegí el motivo de la denuncia.";
                return false;
            }

            var descripcion = (solicitud.Description ?? String.Empty).Trim();
            if (descripcion.Length < MinimoDescripcion || descripcion.Length > MaximoDescripcion)
            {
                error = "Contá qué pasó (entre " + MinimoDescripcion + " y " + MaximoDescripcion + " caracteres).";
                return false;
            }
            if (imagenes.Count > MaximoImagenes)
            {
                error = "Podés adjuntar hasta " + MaximoImagenes + " imágenes.";
                return false;
            }

            var esCliente = solicitud.Actor == WalkCancelledBy.Customer;
            Int64 idDenunciante, idDenunciado;
            String clave;

            Int64 idHospedaje;
            if (AyudanteReservas.EsClaveDeHospedaje(solicitud.BookingKey, out idHospedaje))
            {
                //Un hospedaje se denuncia igual que un paseo: lo pueden denunciar su cliente o su cuidador, una vez confirmado
                var hospedaje = this.repositorioHospedajes.TodosConIncluidos(x => x.Walker, x => x.Customer).FirstOrDefault(x => x.Id == idHospedaje);
                var esDelHospedaje = hospedaje != null && (esCliente ? hospedaje.CustomerId == solicitud.ActorId : hospedaje.WalkerId == solicitud.ActorId);
                if (!esDelHospedaje)
                {
                    error = "El hospedaje no existe.";
                    return false;
                }
                if (hospedaje.Status != WalkStatus.Confirmed)
                {
                    error = "Solo se puede denunciar sobre un hospedaje que el cuidador confirmó.";
                    return false;
                }

                idDenunciante = esCliente ? hospedaje.Customer.UserId : hospedaje.Walker.UserId;
                idDenunciado = esCliente ? hospedaje.Walker.UserId : hospedaje.Customer.UserId;
                clave = solicitud.BookingKey;
            }
            else
            {
                var paseos = AyudanteReservas.Buscar(this.repositorioPaseos.TodosConIncluidos(x => x.Pet), solicitud.BookingKey);
                var esDelActor = paseos.Count > 0 &&
                                 (esCliente
                                     ? paseos[0].Pet.CustomerId == solicitud.ActorId
                                     : paseos[0].WalkerId == solicitud.ActorId);
                if (!esDelActor)
                {
                    error = "La reserva no existe.";
                    return false;
                }
                if (paseos.Any(x => x.Status != WalkStatus.Confirmed))
                {
                    error = "Solo se puede denunciar sobre un paseo que el paseador confirmó.";
                    return false;
                }

                var cliente = this.repositorioClientes.ObtenerUno(paseos[0].Pet.CustomerId);
                var paseador = this.repositorioPaseadores.ObtenerUno(paseos[0].WalkerId);
                if (cliente == null || paseador == null)
                {
                    error = "La reserva no existe.";
                    return false;
                }

                idDenunciante = esCliente ? cliente.UserId : paseador.UserId;
                idDenunciado = esCliente ? paseador.UserId : cliente.UserId;
                clave = AyudanteReservas.ClaveDe(paseos[0]);
            }

            if (this.repositorioDenuncias.BuscarPor(x => x.BookingKey == clave && x.ReporterUserId == idDenunciante && x.Status != ComplaintStatus.Resolved).Any())
            {
                error = "Ya tenés una denuncia abierta sobre este paseo. Un administrador la está revisando.";
                return false;
            }

            var denuncia = new Complaint
            {
                BookingKey = clave,
                ReporterUserId = idDenunciante,
                ReportedUserId = idDenunciado,
                ReporterRole = solicitud.Actor,
                Reason = solicitud.Reason,
                Description = descripcion,
                Status = ComplaintStatus.Open,
                CreatedAt = DateTime.Now
            };
            this.repositorioDenuncias.Agregar(denuncia);
            this.unidadDeTrabajo.GuardarCambios();

            foreach (var imagen in imagenes)
            {
                this.repositorioImagenes.Agregar(new ComplaintImage
                {
                    ComplaintId = denuncia.Id,
                    FileName = imagen.FileName,
                    OriginalName = imagen.OriginalName,
                    ContentType = imagen.ContentType,
                    SizeBytes = imagen.SizeBytes,
                    CreatedAt = DateTime.Now
                });
            }
            if (imagenes.Count > 0)
            {
                this.unidadDeTrabajo.GuardarCambios();
            }

            return true;
        }

        /* ---------- Administrador ---------- */

        public AdminComplaintPageDto Listar(String estado, Int32 pagina, Int32 tamanoPagina)
        {
            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Min(MaximoPagina, Math.Max(1, tamanoPagina));

            IEnumerable<Complaint> consulta = this.repositorioDenuncias.ObtenerTodos().ToList();
            if (estado == ComplaintStatus.Open || estado == ComplaintStatus.InReview || estado == ComplaintStatus.Resolved)
            {
                consulta = consulta.Where(x => x.Status == estado);
            }
            else if (estado == "Active")
            {
                consulta = consulta.Where(x => x.Status != ComplaintStatus.Resolved);
            }

            //Las pendientes: la mas vieja primero (hay que atenderlas en orden); las resueltas: la mas reciente primero
            var ordenadas = consulta.
                OrderBy(x => x.Status == ComplaintStatus.Resolved ? 1 : 0).
                ThenBy(x => x.Status == ComplaintStatus.Resolved ? 0 : x.CreatedAt.Ticks).
                ThenByDescending(x => x.Status == ComplaintStatus.Resolved ? x.CreatedAt.Ticks : 0).
                ToList();

            var pagina_ = ordenadas.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToList();

            return new AdminComplaintPageDto
            {
                Total = ordenadas.Count,
                Page = pagina,
                PageSize = tamanoPagina,
                Complaints = Armar(pagina_)
            };
        }

        public AdminComplaintDto Obtener(Int64 idDenuncia)
        {
            var denuncia = this.repositorioDenuncias.ObtenerUno(idDenuncia);
            return denuncia == null ? null : Armar(new List<Complaint> { denuncia }).First();
        }

        public Boolean PonerEnRevision(Int64 idAdministrador, SetComplaintStatusDto solicitud, out String error)
        {
            error = null;

            var denuncia = solicitud == null ? null : this.repositorioDenuncias.ObtenerUno(solicitud.Id);
            if (denuncia == null)
            {
                error = "La denuncia no existe.";
                return false;
            }
            if (solicitud.Status != ComplaintStatus.InReview || denuncia.Status != ComplaintStatus.Open)
            {
                error = denuncia.Status == ComplaintStatus.Resolved ? "La denuncia ya está resuelta." : "La denuncia ya está en revisión.";
                return false;
            }

            denuncia.Status = ComplaintStatus.InReview;
            Registrar(idAdministrador, AdminActionTypes.ReviewComplaint, denuncia.ReportedUserId, "Denuncia #" + denuncia.Id + " puesta en revisión");
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        public Boolean Resolver(Int64 idAdministrador, ResolveComplaintDto solicitud, out String error)
        {
            error = null;

            var denuncia = solicitud == null ? null : this.repositorioDenuncias.ObtenerUno(solicitud.Id);
            if (denuncia == null)
            {
                error = "La denuncia no existe.";
                return false;
            }
            if (denuncia.Status == ComplaintStatus.Resolved)
            {
                error = "La denuncia ya está resuelta.";
                return false;
            }
            if (solicitud.Resolution != ComplaintResolution.NoAction && solicitud.Resolution != ComplaintResolution.Warning && solicitud.Resolution != ComplaintResolution.Block)
            {
                error = "Elegí cómo se resuelve la denuncia.";
                return false;
            }

            var nota = (solicitud.Note ?? String.Empty).Trim();
            if (nota.Length < MinimoNota || nota.Length > MaximoNota)
            {
                error = "Escribí la nota de la resolución (entre " + MinimoNota + " y " + MaximoNota + " caracteres). " +
                        "Si hay sanción, es el motivo que se le informa a la persona denunciada.";
                return false;
            }

            var denunciado = this.repositorioUsuarios.ObtenerUno(denuncia.ReportedUserId);

            if (solicitud.Resolution == ComplaintResolution.Block && denunciado != null && !denunciado.IsLocked)
            {
                //Bloquear cancela las reservas por empezar, avisa por mail con el motivo y deja el bloqueo en la bitacora
                String errorBloqueo;
                if (!this.servicioAdministracion.Bloquear(idAdministrador, new BlockUserDto { UserId = denuncia.ReportedUserId, Reason = nota }, out errorBloqueo))
                {
                    error = errorBloqueo;
                    return false;
                }
            }

            denuncia.Status = ComplaintStatus.Resolved;
            denuncia.Resolution = solicitud.Resolution;
            denuncia.ResolutionNote = nota;
            denuncia.ResolvedAt = DateTime.Now;
            denuncia.ResolvedByUserId = idAdministrador;

            Registrar(idAdministrador, AdminActionTypes.ResolveComplaint, denuncia.ReportedUserId,
                      "Denuncia #" + denuncia.Id + ": " + TextoResolucion(solicitud.Resolution) + " · " + nota);
            this.unidadDeTrabajo.GuardarCambios();

            if (solicitud.Resolution == ComplaintResolution.Warning && denunciado != null)
            {
                this.servicioNotificaciones.AdvertenciaPorDenuncia(denunciado.Email, nota);
            }

            return true;
        }

        public ComplaintImage ObtenerImagen(Int64 idDenuncia, Int64 idImagen)
        {
            return this.repositorioImagenes.BuscarPor(x => x.Id == idImagen && x.ComplaintId == idDenuncia).FirstOrDefault();
        }

        public List<AdminChatMessageDto> LeerChat(Int64 idAdministrador, Int64 idDenuncia, out String error)
        {
            error = null;

            var denuncia = this.repositorioDenuncias.ObtenerUno(idDenuncia);
            if (denuncia == null)
            {
                error = "La denuncia no existe.";
                return null;
            }

            String nombreCliente = "Cliente", nombrePaseador = "Paseador";
            Int64 idHospedaje;
            if (AyudanteReservas.EsClaveDeHospedaje(denuncia.BookingKey, out idHospedaje))
            {
                var hospedaje = this.repositorioHospedajes.TodosConIncluidos(x => x.Walker, x => x.Customer).FirstOrDefault(x => x.Id == idHospedaje);
                if (hospedaje != null)
                {
                    nombreCliente = NombreCompleto(hospedaje.Customer.FirstName, hospedaje.Customer.LastName);
                    nombrePaseador = NombreCompleto(hospedaje.Walker.FirstName, hospedaje.Walker.LastName);
                }
            }
            else
            {
                var paseos = AyudanteReservas.Buscar(this.repositorioPaseos.TodosConIncluidos(x => x.Pet, x => x.Pet.Customer, x => x.Walker), denuncia.BookingKey);
                nombreCliente = paseos.Count > 0 ? NombreCompleto(paseos[0].Pet.Customer.FirstName, paseos[0].Pet.Customer.LastName) : nombreCliente;
                nombrePaseador = paseos.Count > 0 ? NombreCompleto(paseos[0].Walker.FirstName, paseos[0].Walker.LastName) : nombrePaseador;
            }

            var clave = denuncia.BookingKey;
            var mensajes = this.repositorioMensajes.BuscarPor(x => x.BookingKey == clave).OrderBy(x => x.SentAt).ThenBy(x => x.Id).ToList().
                Select(x => new AdminChatMessageDto
                {
                    Id = x.Id,
                    SenderRole = x.SenderRole,
                    SenderName = x.SenderRole == "Walker" ? nombrePaseador : x.SenderRole == "Customer" ? nombreCliente : "Sistema",
                    Text = x.Text,
                    SentAt = x.SentAt
                }).ToList();

            //Leer una conversacion privada es una accion delicada: queda registrada
            Registrar(idAdministrador, AdminActionTypes.ViewComplaintChat, denuncia.ReportedUserId,
                      "Denuncia #" + denuncia.Id + ": leyó el chat de la reserva (" + mensajes.Count + (mensajes.Count == 1 ? " mensaje)" : " mensajes)"));
            this.unidadDeTrabajo.GuardarCambios();

            return mensajes;
        }

        /* ---------- Armado ---------- */

        private List<AdminComplaintDto> Armar(List<Complaint> denuncias)
        {
            if (denuncias.Count == 0)
            {
                return new List<AdminComplaintDto>();
            }

            var idsUsuarios = denuncias.SelectMany(x => new[] { x.ReporterUserId, x.ReportedUserId }).
                Concat(denuncias.Where(x => x.ResolvedByUserId.HasValue).Select(x => x.ResolvedByUserId.Value)).
                Distinct().ToList();
            var usuarios = this.repositorioUsuarios.BuscarPor(x => idsUsuarios.Contains(x.Id)).ToList().ToDictionary(x => x.Id);
            var clientes = this.repositorioClientes.BuscarPor(x => idsUsuarios.Contains(x.UserId)).ToList().ToDictionary(x => x.UserId);
            var paseadores = this.repositorioPaseadores.BuscarPor(x => idsUsuarios.Contains(x.UserId)).ToList().ToDictionary(x => x.UserId);

            var idsDenuncias = denuncias.Select(x => x.Id).ToList();
            var imagenes = this.repositorioImagenes.BuscarPor(x => idsDenuncias.Contains(x.ComplaintId)).OrderBy(x => x.Id).ToList().
                GroupBy(x => x.ComplaintId).ToDictionary(x => x.Key, x => x.ToList());

            var idsDenunciados = denuncias.Select(x => x.ReportedUserId).Distinct().ToList();
            var recibidas = this.repositorioDenuncias.BuscarPor(x => idsDenunciados.Contains(x.ReportedUserId)).
                Select(x => new { x.Id, x.ReportedUserId }).ToList();

            Func<Int64, String> nombre = idUsuario =>
                paseadores.ContainsKey(idUsuario) ? NombreCompleto(paseadores[idUsuario].FirstName, paseadores[idUsuario].LastName) :
                clientes.ContainsKey(idUsuario) ? NombreCompleto(clientes[idUsuario].FirstName, clientes[idUsuario].LastName) :
                (usuarios.ContainsKey(idUsuario) ? usuarios[idUsuario].Email : "Usuario eliminado");
            Func<Int64, String> email = idUsuario => usuarios.ContainsKey(idUsuario) ? usuarios[idUsuario].Email : null;
            Func<Int64, String> rol = idUsuario => paseadores.ContainsKey(idUsuario) ? "Walker" : "Customer";

            return denuncias.Select(x => new AdminComplaintDto
            {
                Id = x.Id,
                CreatedAt = x.CreatedAt,
                Status = x.Status,
                Reason = x.Reason,
                Description = x.Description,
                BookingKey = x.BookingKey,
                BookingSummary = ResumenDeReserva(x.BookingKey),
                ReporterName = nombre(x.ReporterUserId),
                ReporterEmail = email(x.ReporterUserId),
                ReporterRole = x.ReporterRole,
                ReportedUserId = x.ReportedUserId,
                ReportedName = nombre(x.ReportedUserId),
                ReportedEmail = email(x.ReportedUserId),
                ReportedRole = rol(x.ReportedUserId),
                ReportedIsLocked = usuarios.ContainsKey(x.ReportedUserId) && usuarios[x.ReportedUserId].IsLocked,
                Images = (imagenes.ContainsKey(x.Id) ? imagenes[x.Id] : new List<ComplaintImage>()).
                    Select(i => new AdminComplaintImageDto { Id = i.Id, OriginalName = i.OriginalName, SizeBytes = i.SizeBytes }).ToList(),
                Resolution = x.Resolution,
                ResolutionNote = x.ResolutionNote,
                ResolvedAt = x.ResolvedAt,
                ResolvedByEmail = x.ResolvedByUserId.HasValue ? email(x.ResolvedByUserId.Value) : null,
                OtherComplaintsAgainstReported = recibidas.Count(r => r.ReportedUserId == x.ReportedUserId && r.Id != x.Id)
            }).ToList();
        }

        //"lunes 5 de octubre, 10:00 · Rex, Luna"
        private String ResumenDeReserva(String claveReserva)
        {
            Int64 idHospedaje;
            if (AyudanteReservas.EsClaveDeHospedaje(claveReserva, out idHospedaje))
            {
                //"Hospedaje del 10/10 al 13/10 (3 noches) · Rex, Luna"
                var hospedaje = this.repositorioHospedajes.TodosConIncluidos(x => x.Pets.Select(p => p.Pet)).FirstOrDefault(x => x.Id == idHospedaje);
                return hospedaje == null ? null :
                    "Hospedaje del " + hospedaje.CheckIn.ToString("dd/MM", CulturaEspanola) + " al " + hospedaje.CheckOut.ToString("dd/MM", CulturaEspanola) +
                    " (" + hospedaje.Nights + (hospedaje.Nights == 1 ? " noche" : " noches") + ") · " + String.Join(", ", hospedaje.Pets.Select(p => p.Pet.Name));
            }

            var paseos = AyudanteReservas.Buscar(this.repositorioPaseos.TodosConIncluidos(x => x.Pet), claveReserva);
            if (paseos.Count == 0)
            {
                return null;
            }

            var inicio = AyudanteReservas.InicioDe(paseos[0]);
            return inicio.ToString("dddd d 'de' MMMM", CulturaEspanola) + ", " + inicio.ToString("HH:mm", CulturaEspanola) +
                   " · " + String.Join(", ", paseos.Select(x => x.Pet.Name));
        }

        private static String NombreCompleto(String nombre, String apellido)
        {
            return ((nombre ?? String.Empty) + " " + (apellido ?? String.Empty)).Trim();
        }

        private static String TextoResolucion(String resolucion)
        {
            return resolucion == ComplaintResolution.Block ? "bloqueo" : resolucion == ComplaintResolution.Warning ? "advertencia" : "sin acción";
        }

        //Queda guardada junto con el resto de los cambios de la operacion
        private void Registrar(Int64 idAdministrador, String accion, Int64? idUsuarioAfectado, String detalle)
        {
            this.repositorioAcciones.Agregar(new AdminAction
            {
                AdminUserId = idAdministrador,
                Action = accion,
                TargetUserId = idUsuarioAfectado,
                Detail = detalle,
                CreatedAt = DateTime.Now
            });
        }
    }
}
