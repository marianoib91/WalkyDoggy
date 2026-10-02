using System;
using System.Collections.Generic;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Services.Contracts
{
    //Pagos con retencion: el cliente le paga a WalkyDoggy y el dinero se retiene hasta que el paseo se hace
    public interface IPaymentAppService
    {
        //Crea el pago en Mercado Pago de una reserva confirmada y devuelve la direccion donde el cliente paga
        String CreateCheckout(String bookingKey, Int64 customerId, String returnUrl, out String error);

        //Verifica contra Mercado Pago que la reserva este pagada y, si es asi, deja el pago retenido.
        //Devuelve "approved", "pending", "failed" o "refunded" (se pago una reserva que ya estaba cancelada); null si no se pudo verificar.
        //Si no se informa paymentId se busca el pago de la reserva en Mercado Pago.
        String ConfirmPayment(String bookingKey, String paymentId, out String error);

        //El cliente confirma que el paseo salio bien: el pago se libera al paseador
        Boolean Release(String bookingKey, Int64 customerId, out String error);

        //El cliente reclama un problema con el paseo: el pago queda en revision y no se libera
        Boolean Dispute(String bookingKey, Int64 customerId, String reason, out String error);

        //Devuelve al cliente un pago retenido. Solo cambia las entidades: quien lo llama confirma los cambios.
        Boolean Refund(List<Walk> walks, out String error);

        //Libera los pagos retenidos cuyo paseo termino hace mas de 24 horas sin reclamo
        void ReleaseDuePayments();

        WalkerBalanceDto GetWalkerBalance(Int64 walkerId);
    }
}
