using System;
using System.Collections.Generic;

namespace WalkyDoggy.Entities
{
    //Hospedaje: uno o mas perros de un cliente se quedan varias noches en la casa de un cuidador (un paseador que lo ofrece)
    public class Stay : IEntityBase
    {
        public Int64 Id { get; set; }

        public Int64 WalkerId { get; set; }

        public Int64 CustomerId { get; set; }

        //Dia de ingreso y de salida (sin hora: la entrega se acuerda por el chat)
        public DateTime CheckIn { get; set; }

        public DateTime CheckOut { get; set; }

        public Int32 Nights { get; set; }

        public Int32 DogsCount { get; set; }

        //Precio por noche y por perro vigente al reservar, y el total (noches x perros x precio)
        public Decimal PricePerNight { get; set; }

        public Decimal Total { get; set; }

        //Cash | MercadoPago (ver PaymentMethods), elegido al pedir el hospedaje
        public String PaymentMethod { get; set; }

        //Pending | Paid | Received (ver PaymentStatuses)
        public String PaymentStatus { get; set; }

        //Id del pago en Mercado Pago (va a la cuenta del cuidador) y cuando se pago
        public String PaymentId { get; set; }

        public DateTime? PaidAt { get; set; }

        //Indicaciones del cliente (comida, medicacion, cuidados)
        public String Details { get; set; }

        //Pending | Confirmed | Cancelled (ver WalkStatus)
        public String Status { get; set; }

        //Walker | Customer | System | Admin (ver WalkCancelledBy)
        public String CancelledBy { get; set; }

        public DateTime? StatusChangedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        //El cuidador recibio a los perros (desde ahi ya no se puede cancelar)
        public DateTime? StartedAt { get; set; }

        //El cuidador devolvio a los perros: desde ese momento corresponde pagar
        public DateTime? FinishedAt { get; set; }

        //El cuidador confirmo que cobro
        public DateTime? ReceivedAt { get; set; }

        public Walker Walker { get; set; }

        public Customer Customer { get; set; }

        public ICollection<StayPet> Pets { get; set; }
    }
}
