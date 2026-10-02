using System;
using System.Collections.Generic;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Services.Contracts
{
    //Avisos por mail a los clientes y paseadores cuando cambia el estado de una reserva o de su pago.
    //Los paseos pueden ser de varias reservas: se agrupan por reserva y se manda un mail por reserva.
    //Ninguno de estos metodos lanza excepciones.
    public interface INotificationAppService
    {
        //Al paseador: tiene una solicitud nueva
        void BookingRequested(IEnumerable<Walk> walks);

        //Al cliente: el paseador confirmo (y, si paga con Mercado Pago, que ya puede pagar)
        void BookingConfirmed(IEnumerable<Walk> walks);

        //A la otra parte: la reserva se cancelo (cancelledBy: Walker, Customer, System o NoPayment)
        void BookingCancelled(IEnumerable<Walk> walks, String cancelledBy);

        //Al paseador y al cliente: el pago quedo retenido
        void PaymentHeld(IEnumerable<Walk> walks);

        //Al paseador: el pago se libero y WalkyDoggy se lo debe
        void PaymentReleased(IEnumerable<Walk> walks);

        //Al paseador y al cliente: el cliente reclamo un problema y el pago queda en revision
        void PaymentDisputed(IEnumerable<Walk> walks);
    }
}
