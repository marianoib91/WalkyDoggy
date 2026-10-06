using System;
using System.Collections.Generic;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Services.Contracts
{
    //Denuncias entre clientes y paseadores. Las crea quien denuncia (con imagenes opcionales) y las resuelve un administrador.
    //La persona denunciada NO se entera de la denuncia: solo si hay una sancion (advertencia o bloqueo), y con el motivo.
    //Las acciones del administrador no comprueban el rol: quien llama es responsable de hacerlo (ver IdentidadDelActor).
    public interface IServicioDenuncias
    {
        //Quien denuncia tiene que ser parte de una reserva confirmada. Las imagenes ya deben estar validadas y guardadas en disco.
        Boolean Crear(CreateComplaintDto solicitud, List<ComplaintImageInfo> imagenes, out String error);

        //estado: Open | InReview | Resolved | Active (abiertas y en revision) | null (todas)
        AdminComplaintPageDto Listar(String estado, Int32 pagina, Int32 tamanoPagina);

        //null si no existe
        AdminComplaintDto Obtener(Int64 idDenuncia);

        //Open -> InReview
        Boolean PonerEnRevision(Int64 idAdministrador, SetComplaintStatusDto solicitud, out String error);

        //Cierra la denuncia. Block bloquea la cuenta de la persona denunciada (cancela sus reservas por empezar);
        //Warning y Block le avisan por mail con el motivo.
        Boolean Resolver(Int64 idAdministrador, ResolveComplaintDto solicitud, out String error);

        //null si la imagen no pertenece a esa denuncia
        ComplaintImage ObtenerImagen(Int64 idDenuncia, Int64 idImagen);

        //El chat de la reserva denunciada. Cada lectura queda en la bitacora. null y el motivo en error si no existe la denuncia.
        List<AdminChatMessageDto> LeerChat(Int64 idAdministrador, Int64 idDenuncia, out String error);
    }
}
