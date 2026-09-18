using System;

namespace WalkyDoggy.Application.Criterias
{
    //Confirmar o cancelar una reserva. Actor: Walker o Customer; ActorId: el id del paseador o del cliente.
    public class BookingActionCriteria
    {
        public String BookingKey { get; set; }

        public String Actor { get; set; }

        public Int64 ActorId { get; set; }
    }
}
