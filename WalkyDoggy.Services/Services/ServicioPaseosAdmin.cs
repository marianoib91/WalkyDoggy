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
    public class ServicioPaseosAdmin : IServicioPaseosAdmin
    {
        private const Int32 MinimoMotivo = 5;
        private const Int32 MaximoMotivo = 300;
        private const Int32 MaximoPagina = 50;

        private readonly IRepositorioEntidadBase<Walk> repositorioPaseos;
        private readonly IRepositorioEntidadBase<Stay> repositorioHospedajes;
        private readonly IRepositorioEntidadBase<AdminAction> repositorioAcciones;
        private readonly IServicioNotificaciones servicioNotificaciones;
        private readonly IServicioMensajes servicioMensajes;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;

        public ServicioPaseosAdmin(IRepositorioEntidadBase<Walk> repositorioPaseos,
                                   IRepositorioEntidadBase<Stay> repositorioHospedajes,
                                   IRepositorioEntidadBase<AdminAction> repositorioAcciones,
                                   IServicioNotificaciones servicioNotificaciones,
                                   IServicioMensajes servicioMensajes,
                                   IUnidadDeTrabajo unidadDeTrabajo)
        {
            this.repositorioPaseos = repositorioPaseos;
            this.repositorioHospedajes = repositorioHospedajes;
            this.repositorioAcciones = repositorioAcciones;
            this.servicioNotificaciones = servicioNotificaciones;
            this.servicioMensajes = servicioMensajes;
            this.unidadDeTrabajo = unidadDeTrabajo;
        }

        //En que punto esta una reserva (se deduce de su estado y de las fechas del primer paseo, que comparte con los demas de la reserva)
        public static String EstadoDe(Walk paseo)
        {
            if (paseo.Status == WalkStatus.Cancelled)
            {
                return AdminWalkStatuses.Cancelled;
            }
            if (paseo.Status == WalkStatus.Pending)
            {
                return AdminWalkStatuses.Pending;
            }
            if (paseo.ReceivedAt.HasValue)
            {
                return AdminWalkStatuses.Collected;
            }
            if (paseo.FinishedAt.HasValue)
            {
                return AdminWalkStatuses.ToCollect;
            }
            return paseo.StartedAt.HasValue ? AdminWalkStatuses.InProgress : AdminWalkStatuses.Upcoming;
        }

        public AdminWalkPageDto Listar(String estado, String tipo, DateTime? desde, DateTime? hasta, String buscar, Int32 pagina, Int32 tamanoPagina)
        {
            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Min(MaximoPagina, Math.Max(1, tamanoPagina));

            var consulta = CargarPaseos();
            if (desde.HasValue)
            {
                var dia = desde.Value.Date;
                consulta = consulta.Where(x => x.Date >= dia);
            }
            if (hasta.HasValue)
            {
                var dia = hasta.Value.Date;
                consulta = consulta.Where(x => x.Date <= dia);
            }

            //Paseos y hospedajes en una misma lista (tipo: "Walk", "Stay" o vacio para los dos)
            var elementos = new List<AdminWalkDto>();
            if (tipo != "Stay")
            {
                elementos.AddRange(consulta.ToList().GroupBy(AyudanteReservas.ClaveDe).Select(ArmarReserva));
            }
            if (tipo != "Walk")
            {
                elementos.AddRange(ListarHospedajes(desde, hasta));
            }
            IEnumerable<AdminWalkDto> reservas = elementos;

            if (AdminWalkStatuses.Todos.Contains(estado ?? String.Empty))
            {
                reservas = reservas.Where(x => x.Status == estado);
            }
            if (!String.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                reservas = reservas.Where(x => Contiene(x.CustomerName, texto) || Contiene(x.WalkerName, texto) || Contiene(x.Pets, texto));
            }

            var ordenadas = reservas.OrderByDescending(x => x.Date).ThenByDescending(x => x.TimeFrom).ThenBy(x => x.BookingKey).ToList();

            return new AdminWalkPageDto
            {
                Total = ordenadas.Count,
                Page = pagina,
                PageSize = tamanoPagina,
                Walks = ordenadas.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToList()
            };
        }

        public Boolean Cancelar(Int64 idAdministrador, CancelWalkDto solicitud, out String error)
        {
            error = null;

            if (solicitud == null || String.IsNullOrWhiteSpace(solicitud.BookingKey))
            {
                error = "Faltan datos de la reserva.";
                return false;
            }

            var motivo = (solicitud.Reason ?? String.Empty).Trim();
            if (motivo.Length < MinimoMotivo || motivo.Length > MaximoMotivo)
            {
                error = "Escribí el motivo de la cancelación (entre " + MinimoMotivo + " y " + MaximoMotivo + " caracteres).";
                return false;
            }

            Int64 idHospedaje;
            if (AyudanteReservas.EsClaveDeHospedaje(solicitud.BookingKey, out idHospedaje))
            {
                return CancelarHospedaje(idAdministrador, idHospedaje, motivo, out error);
            }

            var paseos = AyudanteReservas.Buscar(CargarPaseos(), solicitud.BookingKey);
            if (paseos.Count == 0)
            {
                error = "La reserva no existe.";
                return false;
            }
            if (paseos.All(x => x.Status == WalkStatus.Cancelled))
            {
                error = "La reserva ya está cancelada.";
                return false;
            }
            if (paseos.Any(x => x.FinishedAt.HasValue))
            {
                error = "El paseo ya terminó: no se puede cancelar.";
                return false;
            }

            var estabaConfirmada = paseos.Any(x => x.Status == WalkStatus.Confirmed);
            var ahora = DateTime.Now;
            var aCancelar = paseos.Where(x => x.Status != WalkStatus.Cancelled).ToList();
            foreach (var paseo in aCancelar)
            {
                paseo.Status = WalkStatus.Cancelled;
                paseo.Confirmed = false;
                paseo.CancelledBy = WalkCancelledBy.Admin;
                paseo.StatusChangedAt = ahora;
            }

            var primero = paseos[0];
            this.repositorioAcciones.Agregar(new AdminAction
            {
                AdminUserId = idAdministrador,
                Action = AdminActionTypes.CancelWalk,
                Detail = ("Reserva de " + Nombre(primero.Pet.Customer.FirstName, primero.Pet.Customer.LastName) + " con " +
                          Nombre(primero.Walker.FirstName, primero.Walker.LastName) + " (" + AyudanteReservas.InicioDe(primero).ToString("dd/MM/yyyy HH:mm") +
                          ") · Motivo: " + motivo),
                CreatedAt = ahora
            });
            this.unidadDeTrabajo.GuardarCambios();

            this.servicioNotificaciones.ReservaCanceladaPorAdministrador(aCancelar, motivo);
            if (estabaConfirmada)
            {
                this.servicioMensajes.AgregarDelSistema(AyudanteReservas.ClaveDe(primero), "Un administrador canceló esta reserva.");
            }

            return true;
        }

        /* ---------- Auxiliares ---------- */

        private IQueryable<Walk> CargarPaseos()
        {
            return this.repositorioPaseos.TodosConIncluidos(x => x.Pet, x => x.Pet.Customer, x => x.Walker, x => x.Price);
        }

        private static AdminWalkDto ArmarReserva(IGrouping<String, Walk> grupo)
        {
            var primero = grupo.First();
            var estado = EstadoDe(primero);

            return new AdminWalkDto
            {
                Kind = "Walk",
                BookingKey = grupo.Key,
                Date = primero.Date,
                TimeFrom = primero.TimeFrom,
                WalkerName = Nombre(primero.Walker.FirstName, primero.Walker.LastName),
                CustomerName = Nombre(primero.Pet.Customer.FirstName, primero.Pet.Customer.LastName),
                Pets = String.Join(", ", grupo.Select(x => x.Pet.Name)),
                PetCount = grupo.Count(),
                Status = estado,
                CancelledBy = estado == AdminWalkStatuses.Cancelled ? primero.CancelledBy : null,
                PaymentMethod = primero.PaymentMethod,
                PaymentStatus = primero.PaymentStatus,
                Total = grupo.Sum(x => x.Price != null ? x.Price.Amount : 0),
                StartedAt = primero.StartedAt,
                FinishedAt = primero.FinishedAt,
                ReceivedAt = primero.ReceivedAt,
                CanCancel = estado == AdminWalkStatuses.Pending || estado == AdminWalkStatuses.Upcoming || estado == AdminWalkStatuses.InProgress
            };
        }

        //Los hospedajes del periodo (por dia de ingreso), en el mismo formato que las reservas de paseos
        private List<AdminWalkDto> ListarHospedajes(DateTime? desde, DateTime? hasta)
        {
            var consulta = CargarHospedajes();
            if (desde.HasValue)
            {
                var dia = desde.Value.Date;
                consulta = consulta.Where(x => x.CheckIn >= dia);
            }
            if (hasta.HasValue)
            {
                var dia = hasta.Value.Date;
                consulta = consulta.Where(x => x.CheckIn <= dia);
            }

            return consulta.ToList().Select(x =>
            {
                var estado = ServicioHospedajes.EstadoDe(x);
                return new AdminWalkDto
                {
                    Kind = "Stay",
                    BookingKey = ServicioHospedajes.ClaveDe(x),
                    Date = x.CheckIn,
                    TimeFrom = String.Empty,
                    CheckOut = x.CheckOut,
                    Nights = x.Nights,
                    WalkerName = Nombre(x.Walker.FirstName, x.Walker.LastName),
                    CustomerName = Nombre(x.Customer.FirstName, x.Customer.LastName),
                    Pets = String.Join(", ", x.Pets.Select(p => p.Pet.Name)),
                    PetCount = x.DogsCount,
                    Status = estado,
                    CancelledBy = estado == AdminWalkStatuses.Cancelled ? x.CancelledBy : null,
                    PaymentMethod = x.PaymentMethod,
                    PaymentStatus = x.PaymentStatus,
                    Total = (Double)x.Total,
                    StartedAt = x.StartedAt,
                    FinishedAt = x.FinishedAt,
                    ReceivedAt = x.ReceivedAt,
                    CanCancel = estado == AdminWalkStatuses.Pending || estado == AdminWalkStatuses.Upcoming || estado == AdminWalkStatuses.InProgress
                };
            }).ToList();
        }

        private IQueryable<Stay> CargarHospedajes()
        {
            return this.repositorioHospedajes.TodosConIncluidos(x => x.Walker, x => x.Customer, x => x.Pets.Select(p => p.Pet));
        }

        private Boolean CancelarHospedaje(Int64 idAdministrador, Int64 idHospedaje, String motivo, out String error)
        {
            error = null;

            var hospedaje = CargarHospedajes().FirstOrDefault(x => x.Id == idHospedaje);
            if (hospedaje == null)
            {
                error = "El hospedaje no existe.";
                return false;
            }
            if (hospedaje.Status == WalkStatus.Cancelled)
            {
                error = "El hospedaje ya está cancelado.";
                return false;
            }
            if (hospedaje.FinishedAt.HasValue)
            {
                error = "El hospedaje ya terminó: no se puede cancelar.";
                return false;
            }

            var estabaConfirmado = hospedaje.Status == WalkStatus.Confirmed;
            var ahora = DateTime.Now;
            hospedaje.Status = WalkStatus.Cancelled;
            hospedaje.CancelledBy = WalkCancelledBy.Admin;
            hospedaje.StatusChangedAt = ahora;

            this.repositorioAcciones.Agregar(new AdminAction
            {
                AdminUserId = idAdministrador,
                Action = AdminActionTypes.CancelWalk,
                Detail = ("Hospedaje de " + Nombre(hospedaje.Customer.FirstName, hospedaje.Customer.LastName) + " con " +
                          Nombre(hospedaje.Walker.FirstName, hospedaje.Walker.LastName) + " (" + hospedaje.CheckIn.ToString("dd/MM/yyyy") + " al " +
                          hospedaje.CheckOut.ToString("dd/MM/yyyy") + ") · Motivo: " + motivo),
                CreatedAt = ahora
            });
            this.unidadDeTrabajo.GuardarCambios();

            if (estabaConfirmado)
            {
                this.servicioMensajes.AgregarDelSistema(ServicioHospedajes.ClaveDe(hospedaje), "Un administrador canceló este hospedaje.");
            }
            return true;
        }

        private static String Nombre(String nombre, String apellido)
        {
            return ((nombre ?? String.Empty) + " " + (apellido ?? String.Empty)).Trim();
        }

        private static Boolean Contiene(String texto, String buscado)
        {
            return texto != null && texto.IndexOf(buscado, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
    }
}
