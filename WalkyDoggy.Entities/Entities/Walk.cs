using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Entities
{
    public class Walk : IEntityBase
    {
        public Int64 Id { get; set; }

        public Int64 WalkerId { get; set; }

        public Int64 PetId { get; set; }

        public Int64 PriceId { get; set; }

        //En Date se guarda el dia del paseo
        public DateTime Date { get; set; }

        public String TimeFrom { get; set; }

        public String Details { get; set; }

        public Boolean Confirmed { get; set; }

        //Pending | Confirmed | Cancelled (ver WalkStatus). Confirmed se mantiene sincronizado con este estado.
        public String Status { get; set; }

        //Walker | Customer | System (ver WalkCancelledBy)
        public String CancelledBy { get; set; }

        public DateTime? StatusChangedAt { get; set; }

        //Une los paseos (uno por mascota) de una misma reserva
        public Guid? BookingCode { get; set; }

        //Cash | MercadoPago (ver PaymentMethods)
        public String PaymentMethod { get; set; }

        //Pending | Held | Released | Refunded | Disputed (ver PaymentStatuses)
        public String PaymentStatus { get; set; }

        //Id del pago en Mercado Pago. Todos los paseos de una reserva comparten el mismo pago.
        public String PaymentId { get; set; }

        //Cuando se cobro (queda retenido), cuando se libero al paseador y cuando se reembolso al cliente
        public DateTime? PaidAt { get; set; }

        public DateTime? ReleasedAt { get; set; }

        public DateTime? RefundedAt { get; set; }

        //Motivo del reclamo del cliente cuando el pago queda en revision
        public String PaymentDisputeReason { get; set; }

        //Direccion donde se retira a las mascotas. Es una copia de la del cliente o la que eligio para este paseo.
        //Los paseos anteriores a este cambio no la tienen (se usa la del cliente).
        public String PickupStreetName { get; set; }

        public Int64? PickupStreetNumber { get; set; }

        public Int64? PickupCityId { get; set; }

        public String PickupLatitude { get; set; }

        public String PickupLongitude { get; set; }

        [NotMapped]
        public Boolean IsExpired { get; set; }

        public City PickupCity { get; set; }

        public Walker Walker { get; set; }

        public Pet Pet { get; set; }

        public Price Price { get; set; }
    }
}
