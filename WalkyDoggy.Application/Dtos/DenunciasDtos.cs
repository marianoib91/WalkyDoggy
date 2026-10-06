using System;
using System.Collections.Generic;

namespace WalkyDoggy.Application.Dtos
{
    /* ---------- Denuncias ---------- */

    //Lo que manda quien denuncia (junto con las imagenes). Actor: Customer o Walker; ActorId: el id del cliente o del paseador.
    public class CreateComplaintDto
    {
        public String BookingKey { get; set; }

        public String Actor { get; set; }

        public Int64 ActorId { get; set; }

        public String Reason { get; set; }

        public String Description { get; set; }
    }

    //Datos de una imagen ya guardada en disco, que el servicio asocia a la denuncia
    public class ComplaintImageInfo
    {
        public String FileName { get; set; }

        public String OriginalName { get; set; }

        public String ContentType { get; set; }

        public Int32 SizeBytes { get; set; }
    }

    public class AdminComplaintImageDto
    {
        public Int64 Id { get; set; }

        public String OriginalName { get; set; }

        public Int32 SizeBytes { get; set; }
    }

    //Una denuncia tal como la ve el administrador (en la lista y en el detalle)
    public class AdminComplaintDto
    {
        public Int64 Id { get; set; }

        public DateTime CreatedAt { get; set; }

        //Open | InReview | Resolved
        public String Status { get; set; }

        public String Reason { get; set; }

        public String Description { get; set; }

        public String BookingKey { get; set; }

        //Resumen de la reserva (dia, horario y mascotas) para ubicar el hecho
        public String BookingSummary { get; set; }

        public String ReporterName { get; set; }

        public String ReporterEmail { get; set; }

        //Customer | Walker
        public String ReporterRole { get; set; }

        public Int64 ReportedUserId { get; set; }

        public String ReportedName { get; set; }

        public String ReportedEmail { get; set; }

        public String ReportedRole { get; set; }

        public Boolean ReportedIsLocked { get; set; }

        public List<AdminComplaintImageDto> Images { get; set; }

        //NoAction | Warning | Block
        public String Resolution { get; set; }

        public String ResolutionNote { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public String ResolvedByEmail { get; set; }

        //Otras denuncias que recibio la misma persona (para ver si es reincidente)
        public Int32 OtherComplaintsAgainstReported { get; set; }
    }

    public class AdminComplaintPageDto
    {
        public Int32 Total { get; set; }

        public Int32 Page { get; set; }

        public Int32 PageSize { get; set; }

        public List<AdminComplaintDto> Complaints { get; set; }
    }

    public class SetComplaintStatusDto
    {
        public Int64 Id { get; set; }

        //Solo se puede pasar a InReview; para cerrarla se usa ResolveComplaintDto
        public String Status { get; set; }
    }

    public class ResolveComplaintDto
    {
        public Int64 Id { get; set; }

        //NoAction | Warning | Block
        public String Resolution { get; set; }

        //Obligatoria. Si hay sancion (Warning o Block) es el motivo que se le informa a la persona denunciada.
        public String Note { get; set; }
    }

    //Un mensaje del chat de la reserva denunciada, tal como lo lee el administrador
    public class AdminChatMessageDto
    {
        public Int64 Id { get; set; }

        //Walker | Customer | System
        public String SenderRole { get; set; }

        public String SenderName { get; set; }

        public String Text { get; set; }

        public DateTime SentAt { get; set; }
    }

    /* ---------- Moderacion de reseñas ---------- */

    //Una valoracion que un cliente le dio a un paseador
    public class AdminRatingDto
    {
        public Int64 Id { get; set; }

        public DateTime Date { get; set; }

        public Int32 Stars { get; set; }

        public String Comment { get; set; }

        public String CustomerName { get; set; }

        public String WalkerName { get; set; }

        public Boolean Hidden { get; set; }

        public String HiddenReason { get; set; }
    }

    //Una reseña que un paseador le dejo a una mascota
    public class AdminPetReviewDto
    {
        public Int64 Id { get; set; }

        public DateTime Date { get; set; }

        public Int32 Stars { get; set; }

        public String Comment { get; set; }

        public String WalkerName { get; set; }

        public String PetName { get; set; }

        public Boolean Hidden { get; set; }

        public String HiddenReason { get; set; }
    }

    public class AdminRatingPageDto
    {
        public Int32 Total { get; set; }

        public Int32 Page { get; set; }

        public Int32 PageSize { get; set; }

        public List<AdminRatingDto> Ratings { get; set; }
    }

    public class AdminPetReviewPageDto
    {
        public Int32 Total { get; set; }

        public Int32 Page { get; set; }

        public Int32 PageSize { get; set; }

        public List<AdminPetReviewDto> Reviews { get; set; }
    }

    public class HideReviewDto
    {
        public Int64 Id { get; set; }

        //Por que se oculta (queda en la bitacora)
        public String Reason { get; set; }
    }

    public class ShowReviewDto
    {
        public Int64 Id { get; set; }
    }
}
