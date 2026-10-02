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

    //Quien cancelo el paseo. System = cancelacion automatica porque el paseador no respondio a tiempo;
    //NoPayment = cancelacion automatica porque el cliente no pago antes de la hora del paseo
    public static class WalkCancelledBy
    {
        public const String Walker = "Walker";

        public const String Customer = "Customer";

        public const String System = "System";

        public const String NoPayment = "NoPayment";
    }

    public static class PaymentMethods
    {
        public const String Cash = "Cash";

        public const String MercadoPago = "MercadoPago";
    }

    //Con Mercado Pago el cliente le paga a WalkyDoggy, que retiene el dinero hasta que el paseo se hace:
    //Pending (sin pagar) -> Held (cobrado y retenido) -> Released (liberado al paseador, a liquidar)
    //Held -> Refunded (se devolvio al cliente) | Disputed (el cliente reclamo y queda en revision)
    public static class PaymentStatuses
    {
        public const String Pending = "Pending";

        public const String Held = "Held";

        public const String Released = "Released";

        public const String Refunded = "Refunded";

        public const String Disputed = "Disputed";
    }
}
