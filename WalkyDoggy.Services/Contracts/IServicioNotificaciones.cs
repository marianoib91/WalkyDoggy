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

        //A la otra parte: un administrador bloqueo la cuenta de uno de los dos (rolBloqueado: Walker o Customer) y se cancelo la reserva
        void ReservaCanceladaPorBloqueo(IEnumerable<Walk> paseos, String rolBloqueado);

        //A las dos partes: un administrador cancelo la reserva, por este motivo
        void ReservaCanceladaPorAdministrador(IEnumerable<Walk> paseos, String motivo);

        //A la persona: un administrador bloqueo su cuenta, por este motivo
        void CuentaBloqueada(String email, String motivo);

        //A la persona: un administrador revisó una denuncia sobre ella y le manda una advertencia, por este motivo (sin decir quien denuncio)
        void AdvertenciaPorDenuncia(String email, String motivo);

        //A la persona: un administrador volvio a habilitar su cuenta
        void CuentaDesbloqueada(String email);
    }
}
