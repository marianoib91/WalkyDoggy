using System;
using System.Collections.Generic;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    //Valoraciones que los clientes les dan a los paseadores
    public interface IRatingAppService
    {
        //Promedio, cantidad y desglose por estrellas de un paseador
        RatingSummaryDto GetSummary(Int64 walkerId);

        //Valoraciones de un paseador, de a paginas. sort: "recent" (default), "best" o "worst".
        //stars: si se informa, solo las de esa cantidad de estrellas.
        RatingPageDto GetRatings(Int64 walkerId, String sort, Int32? stars, Int32 page, Int32 pageSize);

        //El cliente valora al paseador por una reserva que el paseador ya dio por finalizada. Una valoracion por reserva.
        Boolean Rate(RateRequestDto request, out String error);
    }
}
