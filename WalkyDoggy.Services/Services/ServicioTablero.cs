using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Services.Utilities;

namespace WalkyDoggy.Services.Services
{
    public class ServicioTablero : IServicioTablero
    {
        private const Int32 DiasPorDefecto = 30;
        private const Int32 DiasMaximos = 366;

        //El grafico por dia muestra como mucho los ultimos dias del periodo (si no, no se lee)
        private const Int32 DiasEnElGrafico = 92;

        private const Int32 CantidadDeTopPaseadores = 5;

        private readonly IRepositorioEntidadBase<Walk> repositorioPaseos;
        private readonly IRepositorioEntidadBase<Stay> repositorioHospedajes;
        private readonly IRepositorioEntidadBase<Ranking> repositorioValoraciones;
        private readonly IRepositorioEntidadBase<PetReview> repositorioResenas;
        private readonly IRepositorioEntidadBase<Complaint> repositorioDenuncias;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;
        private readonly IRepositorioEntidadBase<User> repositorioUsuarios;

        public ServicioTablero(IRepositorioEntidadBase<Walk> repositorioPaseos,
                               IRepositorioEntidadBase<Stay> repositorioHospedajes,
                               IRepositorioEntidadBase<Ranking> repositorioValoraciones,
                               IRepositorioEntidadBase<PetReview> repositorioResenas,
                               IRepositorioEntidadBase<Complaint> repositorioDenuncias,
                               IRepositorioEntidadBase<Customer> repositorioClientes,
                               IRepositorioEntidadBase<Walker> repositorioPaseadores,
                               IRepositorioEntidadBase<User> repositorioUsuarios)
        {
            this.repositorioPaseos = repositorioPaseos;
            this.repositorioHospedajes = repositorioHospedajes;
            this.repositorioValoraciones = repositorioValoraciones;
            this.repositorioResenas = repositorioResenas;
            this.repositorioDenuncias = repositorioDenuncias;
            this.repositorioClientes = repositorioClientes;
            this.repositorioPaseadores = repositorioPaseadores;
            this.repositorioUsuarios = repositorioUsuarios;
        }

        //Una reserva con lo que hace falta para las metricas
        private class Reserva
        {
            public String Clave { get; set; }
            public DateTime Dia { get; set; }
            public String Estado { get; set; }
            public String CanceladaPor { get; set; }
            public Int64 IdPaseador { get; set; }
            public String NombrePaseador { get; set; }
            public Int64 IdCliente { get; set; }
            public String MetodoDePago { get; set; }
            public Double Total { get; set; }
            public Int32 Mascotas { get; set; }
            public Boolean Terminada { get; set; }
            public Boolean EsHospedaje { get; set; }
            public Int32 Noches { get; set; }
        }

        public DashboardDto Obtener(DateTime? desde, DateTime? hasta, Decimal comisionPorcentaje)
        {
            var fin = (hasta ?? DateTime.Now).Date;
            var inicio = (desde ?? fin.AddDays(-(DiasPorDefecto - 1))).Date;
            if (inicio > fin)
            {
                var intercambio = inicio;
                inicio = fin;
                fin = intercambio;
            }
            if ((fin - inicio).TotalDays + 1 > DiasMaximos)
            {
                inicio = fin.AddDays(-(DiasMaximos - 1));
            }
            var finExclusivo = fin.AddDays(1);

            var reservas = this.repositorioPaseos.TodosConIncluidos(x => x.Pet, x => x.Walker, x => x.Price).
                Where(x => x.Date >= inicio && x.Date <= fin).
                ToList().
                GroupBy(AyudanteReservas.ClaveDe).
                Select(grupo =>
                {
                    var primero = grupo.First();
                    return new Reserva
                    {
                        Clave = grupo.Key,
                        Dia = primero.Date.Date,
                        Estado = ServicioPaseosAdmin.EstadoDe(primero),
                        CanceladaPor = primero.CancelledBy,
                        IdPaseador = primero.WalkerId,
                        NombrePaseador = ((primero.Walker.FirstName ?? String.Empty) + " " + (primero.Walker.LastName ?? String.Empty)).Trim(),
                        IdCliente = primero.Pet.CustomerId,
                        MetodoDePago = primero.PaymentMethod,
                        Total = grupo.Sum(x => x.Price != null ? x.Price.Amount : 0),
                        Mascotas = grupo.Count(),
                        Terminada = primero.FinishedAt.HasValue && primero.Status != WalkStatus.Cancelled
                    };
                }).
                ToList();

            //Los hospedajes cuentan como reservas (por dia de ingreso) en todas las metricas; ademas tienen las suyas (cantidad, noches y perros hospedados)
            reservas.AddRange(this.repositorioHospedajes.TodosConIncluidos(x => x.Walker).
                Where(x => x.CheckIn >= inicio && x.CheckIn <= fin).
                ToList().
                Select(x => new Reserva
                {
                    Clave = ServicioHospedajes.ClaveDe(x),
                    Dia = x.CheckIn.Date,
                    Estado = ServicioHospedajes.EstadoDe(x),
                    CanceladaPor = x.CancelledBy,
                    IdPaseador = x.WalkerId,
                    NombrePaseador = ((x.Walker.FirstName ?? String.Empty) + " " + (x.Walker.LastName ?? String.Empty)).Trim(),
                    IdCliente = x.CustomerId,
                    MetodoDePago = x.PaymentMethod,
                    Total = (Double)x.Total,
                    Mascotas = x.DogsCount,
                    Terminada = x.FinishedAt.HasValue && x.Status != WalkStatus.Cancelled,
                    EsHospedaje = true,
                    Noches = x.Nights
                }));

            var noCanceladas = reservas.Where(x => x.Estado != AdminWalkStatuses.Cancelled).ToList();
            var canceladas = reservas.Where(x => x.Estado == AdminWalkStatuses.Cancelled).ToList();
            var cobradas = reservas.Where(x => x.Estado == AdminWalkStatuses.Collected).ToList();

            var efectivo = cobradas.Where(x => x.MetodoDePago != PaymentMethods.MercadoPago).Sum(x => x.Total);
            var mercadoPago = cobradas.Where(x => x.MetodoDePago == PaymentMethods.MercadoPago).Sum(x => x.Total);

            var valoraciones = this.repositorioValoraciones.ObtenerTodos().
                Where(x => !x.Hidden && x.Date >= inicio && x.Date < finExclusivo).
                Select(x => new { x.WalkerId, x.Score }).
                ToList();

            var promediosPorPaseador = valoraciones.GroupBy(x => x.WalkerId).ToDictionary(x => x.Key, x => Math.Round(x.Average(v => v.Score), 1));

            var resultado = new DashboardDto
            {
                From = inicio,
                To = fin,
                TotalBookings = reservas.Count,
                ByStatus = AdminWalkStatuses.Todos.Select(estado => new DashboardCountDto { Key = estado, Count = reservas.Count(x => x.Estado == estado) }).ToList(),
                CancelledBy = new[] { WalkCancelledBy.Customer, WalkCancelledBy.Walker, WalkCancelledBy.System, WalkCancelledBy.Admin }.
                    Select(quien => new DashboardCountDto { Key = quien, Count = canceladas.Count(x => x.CanceladaPor == quien) }).ToList(),
                CancellationRate = reservas.Count == 0 ? 0 : Math.Round(100.0 * canceladas.Count / reservas.Count, 1),
                ActiveWalkers = noCanceladas.Select(x => x.IdPaseador).Distinct().Count(),
                ActiveCustomers = noCanceladas.Select(x => x.IdCliente).Distinct().Count(),
                PetsWalked = reservas.Where(x => x.Terminada && !x.EsHospedaje).Sum(x => x.Mascotas),
                StayBookings = noCanceladas.Count(x => x.EsHospedaje),
                StayNights = noCanceladas.Where(x => x.EsHospedaje).Sum(x => x.Noches),
                PetsBoarded = reservas.Where(x => x.Terminada && x.EsHospedaje).Sum(x => x.Mascotas),
                CashCollected = efectivo,
                MercadoPagoCollected = mercadoPago,
                TotalCollected = efectivo + mercadoPago,
                CommissionPercent = comisionPorcentaje,
                EstimatedCommission = comisionPorcentaje > 0 ? Math.Round(mercadoPago * (Double)comisionPorcentaje / 100.0, 2) : (Double?)null,
                RatingCount = valoraciones.Count,
                AverageRating = valoraciones.Count == 0 ? (Double?)null : Math.Round(valoraciones.Average(x => x.Score), 1),
                PetReviewCount = this.repositorioResenas.ObtenerTodos().Count(x => !x.Hidden && x.Date >= inicio && x.Date < finExclusivo),
                PendingComplaints = this.repositorioDenuncias.ObtenerTodos().Count(x => x.Status != ComplaintStatus.Resolved),
                ComplaintsInPeriod = this.repositorioDenuncias.ObtenerTodos().Count(x => x.CreatedAt >= inicio && x.CreatedAt < finExclusivo),
                TotalCustomers = this.repositorioClientes.ObtenerTodos().Count(),
                TotalWalkers = this.repositorioPaseadores.ObtenerTodos().Count(),
                LockedUsers = this.repositorioUsuarios.ObtenerTodos().Count(x => x.IsLocked),
                ByDay = ArmarPorDia(reservas, inicio, fin),
                TopWalkers = noCanceladas.
                    GroupBy(x => x.IdPaseador).
                    Select(x => new DashboardWalkerDto
                    {
                        Name = x.First().NombrePaseador,
                        Bookings = x.Count(),
                        AverageRating = promediosPorPaseador.ContainsKey(x.Key) ? promediosPorPaseador[x.Key] : (Double?)null
                    }).
                    OrderByDescending(x => x.Bookings).
                    ThenBy(x => x.Name).
                    Take(CantidadDeTopPaseadores).
                    ToList()
            };

            return resultado;
        }

        //Una entrada por cada dia (aunque no haya reservas), para dibujar el grafico sin huecos
        private static List<DashboardDayDto> ArmarPorDia(List<Reserva> reservas, DateTime inicio, DateTime fin)
        {
            var primerDia = (fin - inicio).TotalDays + 1 > DiasEnElGrafico ? fin.AddDays(-(DiasEnElGrafico - 1)) : inicio;
            var porDia = reservas.GroupBy(x => x.Dia).ToDictionary(x => x.Key);
            var dias = new List<DashboardDayDto>();

            for (var dia = primerDia; dia <= fin; dia = dia.AddDays(1))
            {
                var delDia = porDia.ContainsKey(dia) ? porDia[dia].ToList() : new List<Reserva>();
                dias.Add(new DashboardDayDto
                {
                    Date = dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Total = delDia.Count,
                    Cancelled = delDia.Count(x => x.Estado == AdminWalkStatuses.Cancelled)
                });
            }

            return dias;
        }
    }
}
