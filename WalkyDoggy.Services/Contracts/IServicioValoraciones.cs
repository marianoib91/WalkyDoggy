using System;
using System.Collections.Generic;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    //Valoraciones que los clientes les dan a los paseadores
    public interface IServicioValoraciones
    {
        //Promedio, cantidad y desglose por estrellas de un paseador
        RatingSummaryDto ObtenerResumen(Int64 idPaseador);

        //Valoraciones de un paseador, de a paginas. sort: "recent" (default), "best" o "worst".
        //stars: si se informa, solo las de esa cantidad de estrellas.
        RatingPageDto ObtenerValoraciones(Int64 idPaseador, String orden, Int32? estrellas, Int32 pagina, Int32 tamanoPagina);

        //El cliente valora al paseador por una reserva que el paseador ya dio por finalizada. Una valoracion por reserva.
        Boolean Valorar(RateRequestDto solicitud, out String error);
    }
}
