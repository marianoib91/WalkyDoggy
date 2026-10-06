using System;
using System.Collections.Generic;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    //Lo que puede hacer un administrador: ver y bloquear usuarios, dar de alta administradores y consultar la bitacora.
    //Quien llama es responsable de comprobar que el usuario sea administrador (ver IdentidadDelActor).
    public interface IServicioAdministracion
    {
        //rol: Customer | Walker | null (todos). estado: Active | Blocked | null (todos). buscar: nombre, mail o telefono.
        AdminUserPageDto ListarUsuarios(String rol, String estado, String buscar, Int32 pagina, Int32 tamanoPagina);

        //Bloquea la cuenta (reversible), cancela sus reservas que todavia no empezaron y avisa por mail
        Boolean Bloquear(Int64 idAdministrador, BlockUserDto solicitud, out String error);

        Boolean Desbloquear(Int64 idAdministrador, UnblockUserDto solicitud, out String error);

        List<AdminAccountDto> ListarAdministradores();

        Boolean CrearAdministrador(Int64 idAdministrador, NewAdminDto solicitud, out String error);

        AdminActionPageDto ListarBitacora(Int32 pagina, Int32 tamanoPagina);
    }
}
