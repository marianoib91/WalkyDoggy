using System;
using System.Collections.Generic;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    //Reseñas que los paseadores dejan de las mascotas que llevaron. Ayudan a otro paseador a decidir cuando le llega un pedido con esa mascota.
    public interface IServicioResenasMascotas
    {
        //Reseña a una mascota de un paseo ya finalizado, hecha por el paseador que lo dio. Devuelve false y el motivo en error si no se puede.
        Boolean Resenar(PetReviewRequestDto solicitud, out String error);

        //Resumen y ultimas reseñas de la mascota; las del paseador que consulta quedan marcadas como propias
        PetReviewSummaryDto ObtenerResenas(Int64 idMascota, Int64 idPaseadorQueConsulta, Int32 maximo);

        //Solo el resumen (cantidad, promedio, negativas) de varias mascotas, por id de mascota
        Dictionary<Int64, PetReviewSummaryDto> ObtenerResumenes(IEnumerable<Int64> idsMascotas);

        //De los paseos indicados (una fila por mascota), los que el paseador ya reseño
        HashSet<Int64> ObtenerPaseosResenados(IEnumerable<Int64> idsPaseos, Int64 idPaseador);
    }
}
