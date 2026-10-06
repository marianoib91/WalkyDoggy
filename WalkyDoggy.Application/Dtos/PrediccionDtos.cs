using System;
using System.Collections.Generic;

namespace WalkyDoggy.Application.Dtos
{
    //Una sugerencia de reserva para un cliente, calculada con su propio historial (sin IA: es una estadistica que se puede explicar)
    public class SugerenciaReservaDto
    {
        //Dia de la semana (0 = domingo ... 6 = sabado) y hora en que suele reservar
        public Int32 DayOfWeek { get; set; }

        public String Time { get; set; }

        //La proxima fecha (yyyy-MM-dd) en que cae ese dia
        public String NextDate { get; set; }

        //Las mascotas que salieron en esa franja la ultima vez
        public List<Int64> PetIds { get; set; }

        public List<String> PetNames { get; set; }

        //Cuantas de las ultimas reservas (Total) caen en ese dia (Matches) y que tan fuerte es el patron (0 a 100)
        public Int32 Matches { get; set; }

        public Int32 Total { get; set; }

        public Int32 Confidence { get; set; }
    }

    //Los dias de la semana en que un paseador suele tener mas reservas (promedio de las ultimas semanas), para que se organice
    public class DemandaPaseadorDto
    {
        //Cuantas semanas y reservas se miraron
        public Int32 Weeks { get; set; }

        public Int32 Total { get; set; }

        //Los dias mas pedidos, de mayor a menor
        public List<DemandaDiaDto> Days { get; set; }
    }

    public class DemandaDiaDto
    {
        //0 = domingo ... 6 = sabado
        public Int32 DayOfWeek { get; set; }

        //Reservas de ese dia en las ultimas semanas
        public Int32 Count { get; set; }

        //La hora en que mas se pide ese dia
        public String PeakTime { get; set; }

        //Fecha (yyyy-MM-dd) de la proxima vez que cae ese dia y cuantas reservas ya tiene para ese dia
        public String NextDate { get; set; }

        public Int32 BookedNext { get; set; }
    }
}
