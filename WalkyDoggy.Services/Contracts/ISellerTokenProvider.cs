using System;

namespace WalkyDoggy.Services.Contracts
{
    //Entrega el access token de la cuenta de Mercado Pago que un paseador vinculo (los tokens se guardan cifrados y se renuevan
    //solos antes de vencer). Devuelve null, con el motivo en error, si no la vinculo o no se pudo renovar.
    public interface ISellerTokenProvider
    {
        //Si el paseador tiene una cuenta de Mercado Pago vinculada
        Boolean IsLinked(Int64 walkerId);

        String GetAccessToken(Int64 walkerId, out String error);
    }
}
