using System;
using System.Collections.Generic;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Services.Contracts
{
    //Avisos por mail a los clientes y paseadores cuando cambia el estado de una reserva.
    //Los paseos pueden ser de varias reservas: se agrupan por reserva y se manda un mail por reserva.
    //Ninguno de estos metodos lanza excepciones.
    public interface IServicioNotificaciones
    {
        //Al paseador: tiene una solicitud nueva
        void ReservaSolicitada(IEnumerable<Walk> paseos);

        //Al cliente: el paseador confirmo (y, si paga con Mercado Pago, cuando va a poder pagar)
        void ReservaConfirmada(IEnumerable<Walk> paseos);

        //A la otra parte: la reserva se cancelo (cancelledBy: Walker, Customer o System)
        void ReservaCancelada(IEnumerable<Walk> paseos, String canceladoPor);
    }
}
