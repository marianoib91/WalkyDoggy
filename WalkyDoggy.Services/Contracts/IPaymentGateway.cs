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

    //Acceso a Mercado Pago. El cliente le paga directamente al paseador: todas las operaciones se hacen con el access token
    //de la cuenta de Mercado Pago del paseador (vinculada por OAuth), y el dinero va a su cuenta.
    public interface IPaymentGateway
    {
        //Crea el pago y devuelve la direccion de la pagina de Mercado Pago donde el cliente paga; null si fallo
        String CreateCheckout(String sellerAccessToken, CheckoutRequest request, out String error);

        //Consulta un pago; null si no existe o no se pudo consultar (en ese caso error trae el motivo)
        GatewayPayment GetPayment(String sellerAccessToken, String paymentId, out String error);

        //Busca el pago aprobado de una reserva; null si todavia no hay ninguno. Si hay varios, prefiere el que coincide con el monto esperado.
        GatewayPayment FindApprovedPayment(String sellerAccessToken, String externalReference, Decimal expectedAmount, out String error);
    }
}
