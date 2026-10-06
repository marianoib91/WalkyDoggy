using System;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    //Moderacion de reseñas: el administrador puede ocultar las valoraciones de paseadores y las reseñas de mascotas que sean ofensivas.
    //Ocultar no borra: la reseña deja de mostrarse y de contar en los promedios, y se puede volver a mostrar.
    //Quien llama es responsable de comprobar que el usuario sea administrador (ver IdentidadDelActor).
    public interface IServicioModeracion
    {
        //estado: Hidden | Visible | null (todas). maximoEstrellas: solo las de hasta esa cantidad (las malas suelen ser las problematicas).
        AdminRatingPageDto ListarValoraciones(String estado, Int32? maximoEstrellas, String buscar, Int32 pagina, Int32 tamanoPagina);

        Boolean OcultarValoracion(Int64 idAdministrador, HideReviewDto solicitud, out String error);

        Boolean MostrarValoracion(Int64 idAdministrador, ShowReviewDto solicitud, out String error);

        AdminPetReviewPageDto ListarResenasMascotas(String estado, Int32? maximoEstrellas, String buscar, Int32 pagina, Int32 tamanoPagina);

        Boolean OcultarResenaMascota(Int64 idAdministrador, HideReviewDto solicitud, out String error);

        Boolean MostrarResenaMascota(Int64 idAdministrador, ShowReviewDto solicitud, out String error);
    }
}
