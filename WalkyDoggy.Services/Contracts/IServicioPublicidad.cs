using System;
using System.Collections.Generic;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    //Publicidad: comercios (pet shops, veterinarias, transporte de mascotas, forrajerias...) y sus avisos.
    //La administracion (comercios y avisos) la hace un administrador; quien llama es responsable de comprobarlo (ver IdentidadDelActor).
    //Los metodos de consulta y de estadisticas (ObtenerActivos, RegistrarVistas, RegistrarClic) los usa cualquier persona que mira la app.
    //Nada se borra: comercios y avisos se dan de baja o se pausan, asi las estadisticas quedan.
    public interface IServicioPublicidad
    {
        List<AdvertiserDto> ListarComercios();

        //Id = 0 crea un comercio; con Id edita sus datos
        Boolean GuardarComercio(Int64 idAdministrador, SaveAdvertiserDto solicitud, out String error);

        //Dar de baja un comercio pausa todos sus avisos (dejan de mostrarse) sin tocar su configuracion
        Boolean EstablecerComercioActivo(Int64 idAdministrador, SetCatalogItemActiveDto solicitud, out String error);

        List<AdminAdDto> ListarAvisos();

        //Id = 0 crea un aviso (devuelve su id en idAviso); con Id edita contenido y condiciones
        Boolean GuardarAviso(Int64 idAdministrador, SaveAdDto solicitud, out Int64 idAviso, out String error);

        //Pausar o reactivar un aviso
        Boolean EstablecerAvisoActivo(Int64 idAdministrador, SetCatalogItemActiveDto solicitud, out String error);

        //Cambia la imagen del aviso (archivoNuevo = nombre ya guardado en disco) o la quita (null).
        //Devuelve en archivoAnterior el archivo que tenia, para que quien llama lo borre del disco.
        Boolean CambiarImagen(Int64 idAdministrador, Int64 idAviso, String archivoNuevo, out String archivoAnterior, out String error);

        //Avisos que hoy corresponde mostrar: activos, dentro de sus fechas, para esa audiencia ("Customers" o "Walkers") y, si tienen
        //alcance por zona, solo si quien mira esta dentro del radio del comercio (hace falta su ubicacion). Primero los menos mostrados.
        List<PublicAdDto> ObtenerActivos(String audiencia, Double? latitud, Double? longitud, Int32 maximo);

        //Directorio "Comercios amigos": todos los comercios activos (tengan o no avisos), con la distancia a quien mira si se sabe donde esta
        //(los mas cercanos primero; si no, por nombre) y las promociones que tienen hoy para esa audiencia y esa ubicacion
        List<PublicAdvertiserDto> ObtenerComerciosAmigos(String audiencia, Double? latitud, Double? longitud);

        void RegistrarVistas(IEnumerable<Int64> idsAvisos);

        Boolean RegistrarClic(Int64 idAviso);
    }
}
