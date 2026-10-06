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
    //Estadistica simple y explicable: el dia de la semana y la hora que mas se repiten en las ultimas reservas del cliente.
    public class ServicioPrediccion : IServicioPrediccion
    {
        //Se miran las ultimas reservas (las costumbres cambian) y hacen falta suficientes para hablar de un patron
        private const Int32 ReservasConsideradas = 8;
        private const Int32 ReservasMinimas = 4;
        private const Int32 CoincidenciasMinimas = 3;
        private const Double ProporcionMinima = 0.6;

        private readonly IRepositorioEntidadBase<Walk> repositorioPaseos;

        public ServicioPrediccion(IRepositorioEntidadBase<Walk> repositorioPaseos)
        {
            this.repositorioPaseos = repositorioPaseos;
        }

        private class Reserva
        {
            public DateTime Inicio { get; set; }
            public List<Pet> Mascotas { get; set; }
        }

        private const Int32 SemanasDeDemanda = 8;
        private const Int32 ReservasMinimasDeDemanda = 6;
        private const Int32 DiasMostrados = 3;

        public DemandaPaseadorDto DemandaDelPaseador(Int64 idPaseador, DateTime hoy)
        {
            var desde = hoy.Date.AddDays(-7 * SemanasDeDemanda);

            //Reservas (agrupadas) que el paseador no rechazo ni cancelo, de las ultimas semanas y las que vienen
            var reservas = this.repositorioPaseos.ObtenerTodos().
                Where(x => x.WalkerId == idPaseador && x.Status != WalkStatus.Cancelled && x.Date >= desde).
                ToList().
                GroupBy(AyudanteReservas.ClaveDe).
                Select(grupo => AyudanteReservas.InicioDe(grupo.First())).
                ToList();

            var historial = reservas.Where(x => x.Date < hoy.Date).ToList();
            if (historial.Count < ReservasMinimasDeDemanda)
            {
                return null;
            }

            //Solo los dias que se repitieron: un dia con una sola reserva no es una tendencia
            var dias = historial.GroupBy(x => x.DayOfWeek).
                Where(g => g.Count() >= 2).
                OrderByDescending(g => g.Count()).
                ThenBy(g => g.Key).
                Take(DiasMostrados).
                Select(g =>
                {
                    var proxima = hoy.Date;
                    while (proxima.DayOfWeek != g.Key || proxima == hoy.Date)
                    {
                        proxima = proxima.AddDays(1);
                    }

                    return new DemandaDiaDto
                    {
                        DayOfWeek = (Int32)g.Key,
                        Count = g.Count(),
                        PeakTime = g.GroupBy(x => x.Hour).OrderByDescending(h => h.Count()).ThenBy(h => h.Key).First().Key.ToString("00", CultureInfo.InvariantCulture) + ":00",
                        NextDate = proxima.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        BookedNext = reservas.Count(x => x.Date == proxima)
                    };
                }).
                ToList();

            if (dias.Count == 0)
            {
                return null;
            }

            return new DemandaPaseadorDto { Weeks = SemanasDeDemanda, Total = historial.Count, Days = dias };
        }

        public SugerenciaReservaDto SugerirReserva(Int64 idCliente, DateTime hoy)
        {
            var reservas = this.repositorioPaseos.TodosConIncluidos(x => x.Pet).
                Where(x => x.Pet.CustomerId == idCliente && x.Status == WalkStatus.Confirmed).
                ToList().
                GroupBy(AyudanteReservas.ClaveDe).
                Select(grupo => new Reserva
                {
                    Inicio = AyudanteReservas.InicioDe(grupo.First()),
                    Mascotas = grupo.Select(x => x.Pet).ToList()
                }).
                ToList();

            //Lo que ya paso: lo que viene no es costumbre, es una reserva que ya hizo
            var historial = reservas.Where(x => x.Inicio.Date <= hoy.Date).
                OrderByDescending(x => x.Inicio).
                Take(ReservasConsideradas).
                ToList();
            if (historial.Count < ReservasMinimas)
            {
                return null;
            }

            var diaFrecuente = historial.GroupBy(x => x.Inicio.DayOfWeek).
                OrderByDescending(g => g.Count()).
                ThenByDescending(g => g.Max(x => x.Inicio)).
                First();
            var coincidencias = diaFrecuente.Count();
            if (coincidencias < CoincidenciasMinimas || (Double)coincidencias / historial.Count < ProporcionMinima)
            {
                return null;
            }

            var hora = diaFrecuente.GroupBy(x => x.Inicio.Hour).
                OrderByDescending(g => g.Count()).
                ThenByDescending(g => g.Max(x => x.Inicio)).
                First().Key;

            //La proxima vez que cae ese dia (desde manana)
            var proxima = hoy.Date.AddDays(1);
            while (proxima.DayOfWeek != diaFrecuente.Key)
            {
                proxima = proxima.AddDays(1);
            }

            //Si ya reservo ese dia no hace falta sugerirlo
            if (reservas.Any(x => x.Inicio.Date == proxima))
            {
                return null;
            }

            var ultima = diaFrecuente.OrderByDescending(x => x.Inicio).First();

            return new SugerenciaReservaDto
            {
                DayOfWeek = (Int32)diaFrecuente.Key,
                Time = hora.ToString("00", CultureInfo.InvariantCulture) + ":00",
                NextDate = proxima.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                PetIds = ultima.Mascotas.Select(x => x.Id).ToList(),
                PetNames = ultima.Mascotas.Select(x => x.Name).ToList(),
                Matches = coincidencias,
                Total = historial.Count,
                Confidence = (Int32)Math.Round(100.0 * coincidencias / historial.Count)
            };
        }
    }
}
