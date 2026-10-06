using System;

namespace WalkyDoggy.Entities
{
    //Mensaje del chat de una reserva. Solo participan el paseador y el cliente; los avisos automaticos (reserva confirmada,
    //paseo iniciado, paseo finalizado) los manda el sistema.
    public class Message : IEntityBase
    {
        public Int64 Id { get; set; }

        //La reserva (BookingCode sin guiones, o "w" + Id del paseo en reservas viejas)
        public String BookingKey { get; set; }

        //Walker | Customer | System
        public String SenderRole { get; set; }

        //Id del paseador o del cliente que lo escribio (null en los avisos del sistema)
        public Int64? SenderId { get; set; }

        public String Text { get; set; }

        public DateTime SentAt { get; set; }

        //Cuando lo leyo la otra persona (null mientras no lo haya leido)
        public DateTime? ReadAt { get; set; }
    }
}
