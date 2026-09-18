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

    public static class PaymentStatuses
    {
        public const String Pending = "Pending";

        public const String Paid = "Paid";
    }
}
