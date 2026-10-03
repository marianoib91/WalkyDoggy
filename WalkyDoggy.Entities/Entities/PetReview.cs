using System;

namespace WalkyDoggy.Entities
{
    //Reseña que un paseador deja de una mascota que llevó a un paseo (como la valoracion que el cliente hace del paseador).
    //Hay una por paseo y por mascota; sirve para que otros paseadores decidan al recibir un pedido con esa mascota.
    public class PetReview : IEntityBase
    {
        public Int64 Id { get; set; }

        //El paseo (una fila por mascota) que se reseña
        public Int64 WalkId { get; set; }

        public Int64 PetId { get; set; }

        public Int64 WalkerId { get; set; }

        public String BookingKey { get; set; }

        public DateTime Date { get; set; }

        //De 1 a 5 estrellas
        public Int32 Stars { get; set; }

        public String Comments { get; set; }
    }
}
