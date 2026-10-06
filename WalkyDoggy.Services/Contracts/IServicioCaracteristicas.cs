using System;
using System.Collections.Generic;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    //Catalogo de caracteristicas de las mascotas (pares opuestos). Lo edita el administrador (ver IServicioCatalogos).
    public interface IServicioCaracteristicas
    {
        //Los pares que se ofrecen al cargar una mascota (solo los activos), en orden
        List<TraitPairDto> ObtenerPares();

        //Codigos de las caracteristicas activas, en el orden de los pares
        List<String> CodigosActivos();

        //Controla las caracteristicas marcadas (existen, una sola por par) y devuelve el motivo del error, o null si esta bien.
        //Las dadas de baja se descartan sin error. En normalizadas queda la lista en el orden de los pares (o null si no hay ninguna).
        String Validar(String guardadas, out String normalizadas);
    }
}
