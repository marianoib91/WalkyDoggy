using System;

namespace WalkyDoggy.Entities
{
    //Denuncia que un cliente o un paseador hace sobre la otra parte de una reserva (maltrato a una mascota, trato irrespetuoso, etc.).
    //La ve y la resuelve un administrador; la persona denunciada NO se entera, salvo que haya una sancion.
    public class Complaint : IEntityBase
    {
        public Int64 Id { get; set; }

        //La reserva sobre la que se denuncia (BookingCode sin guiones, o "w" + Id del paseo en reservas viejas)
        public String BookingKey { get; set; }

        public Int64 ReporterUserId { get; set; }

        public Int64 ReportedUserId { get; set; }

        //Customer | Walker: quien denuncia
        public String ReporterRole { get; set; }

        //Ver ComplaintReasons
        public String Reason { get; set; }

        public String Description { get; set; }

        //Open | InReview | Resolved (ver ComplaintStatus)
        public String Status { get; set; }

        //NoAction | Warning | Block (ver ComplaintResolution); null mientras no se resuelve
        public String Resolution { get; set; }

        //Lo que escribe el administrador al resolver; si hay sancion, es el motivo que se le informa a la persona denunciada
        public String ResolutionNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public Int64? ResolvedByUserId { get; set; }
    }
}
