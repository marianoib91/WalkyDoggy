using System;
using System.Collections.Generic;

namespace WalkyDoggy.Application.Dtos
{
    //Resumen de las valoraciones de un paseador: promedio, cantidad y desglose por estrellas
    public class RatingSummaryDto
    {
        public Int64 WalkerId { get; set; }

        //Promedio redondeado a un decimal; null si todavia no tiene valoraciones
        public Double? Average { get; set; }

        public Int32 Count { get; set; }

        //Cuantas valoraciones tienen comentario
        public Int32 CommentCount { get; set; }

        //Cantidad de valoraciones de 5, 4, 3, 2 y 1 estrellas (en ese orden)
        public List<RatingBucketDto> Distribution { get; set; }
    }

    public class RatingBucketDto
    {
        public Int32 Stars { get; set; }

        public Int32 Count { get; set; }
    }

    public class RatingDto
    {
        public Int64 Id { get; set; }

        public Int32 Stars { get; set; }

        public String Comment { get; set; }

        //Nombre del cliente con la inicial del apellido ("Mariano B."), para no mostrar el nombre completo
        public String AuthorName { get; set; }

        public DateTime Date { get; set; }
    }

    public class RatingPageDto
    {
        public List<RatingDto> Items { get; set; }

        //Total de valoraciones que cumplen el filtro (no solo las de esta pagina)
        public Int32 Total { get; set; }

        public Int32 Page { get; set; }

        public Boolean HasMore { get; set; }
    }

    //Pedido para valorar una reserva
    public class RateRequestDto
    {
        public String BookingKey { get; set; }

        public Int64 CustomerId { get; set; }

        public Int32 Stars { get; set; }

        public String Comment { get; set; }
    }

    //Franjas horarias de la semana de un paseador: reemplazan por completo las que tenia
    public class WorkWeekDto
    {
        public Int64 WalkerId { get; set; }

        public List<WorkRangeDto> Ranges { get; set; }
    }

    public class WorkRangeDto
    {
        //Lunes, Martes, Miércoles, Jueves, Viernes, Sábado o Domingo
        public String DayOfWeek { get; set; }

        //Horas en punto: "08:00". El fin puede ser "24:00" (medianoche).
        public String TimeFrom { get; set; }

        public String TimeUntil { get; set; }
    }
}
