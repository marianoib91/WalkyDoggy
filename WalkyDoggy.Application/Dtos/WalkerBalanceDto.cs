using System;

namespace WalkyDoggy.Application.Dtos
{
    //Lo que WalkyDoggy tiene cobrado a nombre del paseador con Mercado Pago, segun en que punto esta cada pago
    public class WalkerBalanceDto
    {
        //Cobrado a los clientes, pero el paseo todavia no se confirmo como realizado
        public Double Retained { get; set; }

        //Paseos realizados cuyo pago ya se libero: WalkyDoggy se lo debe al paseador
        public Double ToSettle { get; set; }

        //Pagos que el cliente reclamo y estan en revision
        public Double InReview { get; set; }

        //Adonde se le transfiere: null si todavia no cargo sus datos de cobro
        public String PayoutAccount { get; set; }

        public String PayoutHolder { get; set; }

        public Boolean HasPayoutAccount { get; set; }
    }

    //Datos de cobro del paseador (alias o CBU/CVU y titular)
    public class PayoutAccountDto
    {
        public String Account { get; set; }

        public String Holder { get; set; }
    }
}
