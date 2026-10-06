using System;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    //El administrador ve todas las reservas del sistema y, en un caso excepcional, puede cancelar una.
    //Quien llama es responsable de comprobar que el usuario sea administrador (ver IdentidadDelActor).
    public interface IServicioPaseosAdmin
    {
        //estado: Pending | Upcoming | InProgress | ToCollect | Collected | Cancelled | null (todos). desde/hasta: dia del paseo (inclusive).
        //buscar: nombre del cliente, del paseador o de una mascota.
        AdminWalkPageDto Listar(String estado, DateTime? desde, DateTime? hasta, String buscar, Int32 pagina, Int32 tamanoPagina);

        //Cancela una reserva que todavia no terminó (CancelledBy = Admin), avisa por mail a las dos partes y lo deja en la bitacora
        Boolean Cancelar(Int64 idAdministrador, CancelWalkDto solicitud, out String error);
    }
}
