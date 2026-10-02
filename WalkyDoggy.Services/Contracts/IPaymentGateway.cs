using System;

namespace WalkyDoggy.Services.Contracts
{
    public class CheckoutRequest
    {
        public String Title { get; set; }

        public Decimal Amount { get; set; }

        //Identifica la reserva en el pago. Mercado Pago lo devuelve y asi se sabe a que reserva corresponde.
        public String ExternalReference { get; set; }

        //Direccion a la que Mercado Pago devuelve al cliente despues de pagar
        public String ReturnUrl { get; set; }
    }

    public class GatewayPayment
    {
        public String Id { get; set; }

        //approved | pending | in_process | rejected | cancelled | refunded ...
        public String Status { get; set; }

        public String ExternalReference { get; set; }

        public Decimal Amount { get; set; }

        public String Currency { get; set; }
    }

    //Acceso a la pasarela de pagos (Mercado Pago). Todos los pagos entran a la cuenta de WalkyDoggy.
    public interface IPaymentGateway
    {
        Boolean IsConfigured { get; }

        //Crea el pago y devuelve la direccion de la pagina de Mercado Pago donde el cliente paga; null si fallo
        String CreateCheckout(CheckoutRequest request, out String error);

        //Consulta un pago; null si no existe o no se pudo consultar (en ese caso error trae el motivo)
        GatewayPayment GetPayment(String paymentId, out String error);

        //Busca el pago aprobado de una reserva; null si todavia no hay ninguno
        GatewayPayment FindApprovedPayment(String externalReference, out String error);

        //Devuelve el pago completo al cliente
        Boolean Refund(String paymentId, out String error);
    }
}
