using System;

namespace WalkyDoggy.Application.Dtos
{
    //Un mensaje del chat de una reserva
    public class MessageDto
    {
        public Int64 Id { get; set; }

        //Walker | Customer | System
        public String SenderRole { get; set; }

        public String Text { get; set; }

        public DateTime SentAt { get; set; }

        //Si ya lo leyo la otra persona (sirve para mostrar "visto" en los mensajes propios)
        public Boolean Read { get; set; }
    }

    //Pedido para escribir un mensaje. Actor: Walker o Customer; ActorId: el id del paseador o del cliente.
    public class SendMessageDto
    {
        public String BookingKey { get; set; }

        public String Actor { get; set; }

        public Int64 ActorId { get; set; }

        public String Text { get; set; }
    }

    //Los mensajes de una reserva y si todavia se puede escribir
    public class ChatDto
    {
        public System.Collections.Generic.List<MessageDto> Messages { get; set; }

        //Se puede escribir mientras la reserva este confirmada y no cerrada (cobrada)
        public Boolean CanWrite { get; set; }
    }
}
