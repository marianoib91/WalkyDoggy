using System;
using System.Collections.Generic;

namespace WalkyDoggy.Application.Dtos
{
    //Una reserva: los paseos (uno por mascota) que salieron de una misma solicitud
    public class BookingDto
    {
        //Identifica la reserva: el BookingCode o, en paseos anteriores a las reservas agrupadas, "w" + Id del paseo
        public String BookingKey { get; set; }

        public Int64 WalkerId { get; set; }

        public String WalkerName { get; set; }

        public String WalkerProfileImage { get; set; }

        public Int64 CustomerId { get; set; }

        public String CustomerFullName { get; set; }

        public DateTime Date { get; set; }

        public String TimeFrom { get; set; }

        public String Status { get; set; }

        public String CancelledBy { get; set; }

        public String PaymentMethod { get; set; }

        public String PaymentStatus { get; set; }

        //El paseador dio por finalizado el paseo; null mientras no lo haga
        public DateTime? FinishedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        //El paseador confirmo que recibio el pago; null mientras no lo haga
        public DateTime? ReceivedAt { get; set; }

        //El cliente ya valoro al paseador por este paseo y con cuantas estrellas (null si todavia no)
        public Int32? RatingStars { get; set; }

        public String Details { get; set; }

        public String Location { get; set; }

        public String Latitude { get; set; }

        public String Longitude { get; set; }

        public Double PricePerPet { get; set; }

        public Double Total { get; set; }

        public List<BookingPetDto> Pets { get; set; }
    }

    public class BookingPetDto
    {
        public Int64 Id { get; set; }

        public String Name { get; set; }

        public String ProfileImage { get; set; }
    }
}
