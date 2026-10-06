using System;
using System.Collections.Generic;

namespace WalkyDoggy.Application.Dtos
{
    //Lo que el paseador ofrece como cuidador: perros varias noches en su casa
    public class BoardingOfferDto
    {
        public Int64 WalkerId { get; set; }

        public Boolean Enabled { get; set; }

        //Por noche y por perro
        public Decimal? PricePerNight { get; set; }

        //Perros que cuida a la vez
        public Int32 MaxDogs { get; set; }

        //Como es el lugar (patio, otros animales, rutina)
        public String Description { get; set; }
    }

    //Un cuidador disponible para unas fechas, tal como lo ve el cliente al buscar
    public class BoardingCarerDto
    {
        public Int64 WalkerId { get; set; }

        public String Name { get; set; }

        public String ProfileImage { get; set; }

        public String CityName { get; set; }

        //Presentacion del paseador y descripcion de su casa para el hospedaje
        public String About { get; set; }

        public String BoardingDescription { get; set; }

        public Decimal PricePerNight { get; set; }

        public Int32 MaxDogs { get; set; }

        //Lugares libres en la noche mas ocupada del periodo
        public Int32 FreeSpots { get; set; }

        public Int32 Nights { get; set; }

        public Int32 Dogs { get; set; }

        //Noches x perros x precio por noche
        public Decimal Total { get; set; }

        //Distancia entre su zona y el domicilio del cliente (null si no se conoce)
        public Double? DistanceKm { get; set; }

        public Double Rating { get; set; }

        public Int32 RatingCount { get; set; }

        //Si vinculo su cuenta de Mercado Pago (si no, el hospedaje solo se paga en efectivo)
        public Boolean MercadoPagoLinked { get; set; }
    }

    public class StayRequestDto
    {
        public Int64 CustomerId { get; set; }

        public Int64 WalkerId { get; set; }

        public List<Int64> PetIds { get; set; }

        //yyyy-MM-dd
        public String CheckIn { get; set; }

        public String CheckOut { get; set; }

        public String Details { get; set; }

        //Cash (por defecto) o MercadoPago (solo si el cuidador vinculo su cuenta)
        public String PaymentMethod { get; set; }
    }

    public class StayPetDto
    {
        public Int64 Id { get; set; }

        public String Name { get; set; }

        public String ProfileImage { get; set; }

        //Si el cuidador ya reseño a este perro por este hospedaje
        public Boolean ReviewedByWalker { get; set; }
    }

    //Un hospedaje en las listas del cuidador y del cliente
    public class StayDto
    {
        public Int64 Id { get; set; }

        //"h" + Id: es la clave del chat (no se mezcla con las de los paseos)
        public String BookingKey { get; set; }

        //Pending | Upcoming | InProgress | ToCollect | Collected | Cancelled (ver AdminWalkStatuses)
        public String Status { get; set; }

        public String CancelledBy { get; set; }

        public Int64 WalkerId { get; set; }

        public String WalkerName { get; set; }

        public String WalkerImage { get; set; }

        public Int64 CustomerId { get; set; }

        public String CustomerName { get; set; }

        public String CustomerImage { get; set; }

        //Se muestra a la otra parte cuando el hospedaje esta confirmado
        public String WalkerPhone { get; set; }

        public String CustomerPhone { get; set; }

        public List<StayPetDto> Pets { get; set; }

        public DateTime CheckIn { get; set; }

        public DateTime CheckOut { get; set; }

        public Int32 Nights { get; set; }

        public Int32 DogsCount { get; set; }

        public Decimal PricePerNight { get; set; }

        public Decimal Total { get; set; }

        public String Details { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public DateTime? ReceivedAt { get; set; }

        //Alias o CBU/CVU del cuidador, para que el cliente le transfiera cuando termina
        public String WalkerPayoutAccount { get; set; }

        //Cash | MercadoPago y Pending | Paid | Received
        public String PaymentMethod { get; set; }

        public String PaymentStatus { get; set; }

        //Estrellas con las que el cliente valoro al cuidador por este hospedaje (null si todavia no)
        public Int32? RatingStars { get; set; }

        public Int32 ChatMessages { get; set; }

        public Int32 ChatUnread { get; set; }
    }
}
