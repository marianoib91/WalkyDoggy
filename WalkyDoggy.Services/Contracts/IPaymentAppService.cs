using System;

namespace WalkyDoggy.Services.Contracts
{
    //Pago de un paseo con Mercado Pago. Se paga despues del paseo y el dinero va directo a la cuenta del paseador.
    public interface IPaymentAppService
    {
        //Crea el pago en Mercado Pago de una reserva que el paseador ya dio por finalizada y devuelve la direccion donde el cliente paga
        String CreateCheckout(String bookingKey, Int64 customerId, String returnUrl, out String error);

        //Verifica contra Mercado Pago que la reserva este pagada y, si es asi, la marca como pagada.
        //Devuelve "approved", "pending" o "failed"; null si no se pudo verificar (el motivo queda en error).
        //Si no se informa paymentId se busca el pago de la reserva en Mercado Pago.
        String ConfirmPayment(String bookingKey, String paymentId, out String error);
    }
}
