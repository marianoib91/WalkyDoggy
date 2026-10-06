using System;
using System.Collections.Generic;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    //Cuantos mensajes tiene el chat de una reserva y cuantos le faltan leer a quien lo consulta
    public class ResumenDeChat
    {
        public Int32 NoLeidos { get; set; }

        public Int32 Total { get; set; }
    }

    //Chat entre el paseador y el cliente de una reserva. Se habilita cuando el paseador la confirma, para ponerse de acuerdo
    //(por ejemplo, en el horario real en que el paseador pasa a buscar a la mascota). Los avisos automaticos los manda el sistema.
    public interface IServicioMensajes
    {
        //Mensajes de la reserva con id mayor a despuesDeId (0 = todos). Lo que escribio la otra persona queda como leido.
        //actor: "Walker" o "Customer"; idActor: el id del paseador o del cliente. Devuelve null y el motivo en error si no corresponde.
        ChatDto ObtenerChat(String claveReserva, String actor, Int64 idActor, Int64 despuesDeId, out String error);

        //Escribe un mensaje en el chat. Devuelve null y el motivo en error si no se puede (chat no habilitado o cerrado, texto vacio o muy largo).
        MessageDto Enviar(SendMessageDto solicitud, out String error);

        //Aviso automatico en el chat de la reserva
        void AgregarDelSistema(String claveReserva, String texto);

        //Mensajes totales y sin leer de varias reservas, para quien las mira (rol: "Walker" o "Customer")
        Dictionary<String, ResumenDeChat> ObtenerResumenes(IEnumerable<String> clavesReserva, String rol);
    }
}
