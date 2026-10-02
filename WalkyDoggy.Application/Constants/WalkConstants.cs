using System;

namespace WalkyDoggy.Application.Constants
{
    //Estado de un paseo: espera la respuesta del paseador, esta confirmado o fue cancelado
    public static class WalkStatus
    {
        public const String Pending = "Pending";

        public const String Confirmed = "Confirmed";

        public const String Cancelled = "Cancelled";
    }

    //Quien cancelo el paseo. System = cancelacion automatica porque el paseador no respondio a tiempo
    public static class WalkCancelledBy
    {
        public const String Walker = "Walker";

        public const String Customer = "Customer";

        public const String System = "System";
    }

    public static class PaymentMethods
    {
        public const String Cash = "Cash";

        public const String MercadoPago = "MercadoPago";
    }

    //El pago se hace DESPUES del paseo y va directo al paseador: cuando el paseador da por finalizado el paseo el cliente paga
    //(con Mercado Pago desde la app, o en efectivo en mano) y el paseador confirma que lo recibio.
    //Pending (sin pagar) -> Paid (el cliente pago con Mercado Pago y se verifico) -> Received (el paseador confirmo que lo recibio)
    //Con efectivo no hay Paid: el paseador confirma Received directamente.
    public static class PaymentStatuses
    {
        public const String Pending = "Pending";

        public const String Paid = "Paid";

        public const String Received = "Received";
    }
}
