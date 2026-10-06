using System;
using System.Collections.Generic;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    //Administracion de los catalogos: razas, tamaños y caracteristicas de las mascotas.
    //Nada se borra: se da de baja, asi las mascotas que ya lo tenian cargado no se rompen.
    //Quien llama es responsable de comprobar que el usuario sea administrador (ver IdentidadDelActor).
    public interface IServicioCatalogos
    {
        List<AdminCatalogItemDto> ListarRazas();

        //Id = 0 crea una raza; con Id cambia el nombre
        Boolean GuardarRaza(Int64 idAdministrador, SaveCatalogItemDto solicitud, out String error);

        Boolean EstablecerRazaActiva(Int64 idAdministrador, SetCatalogItemActiveDto solicitud, out String error);

        List<AdminCatalogItemDto> ListarTamanos();

        Boolean GuardarTamano(Int64 idAdministrador, SaveCatalogItemDto solicitud, out String error);

        Boolean EstablecerTamanoActivo(Int64 idAdministrador, SetCatalogItemActiveDto solicitud, out String error);

        //Todos los pares (activos y dados de baja), con cuantas mascotas los usan
        List<TraitPairDto> ListarCaracteristicas();

        Boolean GuardarCaracteristicas(Int64 idAdministrador, SaveTraitPairDto solicitud, out String error);

        Boolean EstablecerParActivo(Int64 idAdministrador, SetTraitPairActiveDto solicitud, out String error);

        //Al cargar o editar una mascota: la raza y el tamaño tienen que existir y no estar dados de baja
        //(si se edita una mascota que ya tenia una raza o tamaño dado de baja y no se cambia, se acepta)
        Boolean RazaYTamanoDisponibles(Int64 idMascota, Int64 idRaza, Int64 idTamano, out String error);
    }
}
