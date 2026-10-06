using System;
using System.Collections.Generic;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    //Paseadores favoritos de cada cliente
    public interface IServicioFavoritos
    {
        //Ids de los paseadores que el cliente marco como favoritos
        List<Int64> ObtenerIdsPaseadores(Int64 idCliente);

        //Marca o desmarca al paseador como favorito (si ya estaba en ese estado no hace nada). Devuelve false y el motivo en error si el paseador no existe.
        Boolean Establecer(SetFavoriteDto solicitud, out String error);
    }
}
