using System;
using System.Collections.Generic;

namespace WalkyDoggy.Application.Dtos
{
    //Lo que manda el paseador para reseñar a una mascota que llevo en un paseo ya finalizado
    public class PetReviewRequestDto
    {
        public String BookingKey { get; set; }

        public Int64 WalkerId { get; set; }

        public Int64 PetId { get; set; }

        //De 1 a 5
        public Int32 Stars { get; set; }

        public String Comment { get; set; }
    }

    //Una reseña de una mascota
    public class PetReviewDto
    {
        public Int32 Stars { get; set; }

        public String Comment { get; set; }

        public DateTime Date { get; set; }

        //El paseador que la escribio, como "Camila R."
        public String WalkerName { get; set; }

        //Si la escribio el paseador que consulta (para destacarla)
        public Boolean IsMine { get; set; }
    }

    //Resumen de las reseñas de una mascota y, al pedir el detalle, las mas recientes
    public class PetReviewSummaryDto
    {
        public Int64 PetId { get; set; }

        public Int32 Count { get; set; }

        //Promedio de estrellas (null si todavia no tiene reseñas)
        public Double? Average { get; set; }

        //Reseñas de 1 o 2 estrellas
        public Int32 NegativeCount { get; set; }

        public List<PetReviewDto> Reviews { get; set; }
    }
}
