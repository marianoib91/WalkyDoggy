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
        private readonly IRepositorioEntidadBase<AdminAction> repositorioAcciones;
        private readonly IServicioNotificaciones servicioNotificaciones;
        private readonly IServicioMensajes servicioMensajes;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;

        public ServicioPaseosAdmin(IRepositorioEntidadBase<Walk> repositorioPaseos,
                                   IRepositorioEntidadBase<AdminAction> repositorioAcciones,
                                   IServicioNotificaciones servicioNotificaciones,
                                   IServicioMensajes servicioMensajes,
                                   IUnidadDeTrabajo unidadDeTrabajo)
        {
            this.repositorioPaseos = repositorioPaseos;
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

        public AdminWalkPageDto Listar(String estado, DateTime? desde, DateTime? hasta, String buscar, Int32 pagina, Int32 tamanoPagina)
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

            var reservas = consulta.ToList().GroupBy(AyudanteReservas.ClaveDe).Select(ArmarReserva);

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
