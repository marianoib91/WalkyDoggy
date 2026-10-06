using System;
using System.Collections.Generic;

namespace WalkyDoggy.Application.Dtos
{
    //Una fila de la lista de usuarios del administrador (cliente o paseador)
    public class AdminUserDto
    {
        public Int64 UserId { get; set; }

        //Customer | Walker
        public String Role { get; set; }

        //Id del cliente o del paseador, segun el rol
        public Int64 ProfileId { get; set; }

        public String FullName { get; set; }

        public String Email { get; set; }

        public String Phone { get; set; }

        public DateTime CreatedDate { get; set; }

        public Boolean IsLocked { get; set; }

        public String BlockReason { get; set; }

        public DateTime? BlockedAt { get; set; }

        //Reservas que todavia no empezaron (pendientes o confirmadas): se cancelan si se bloquea la cuenta
        public Int32 UpcomingBookings { get; set; }
    }

    public class AdminUserPageDto
    {
        public Int32 Total { get; set; }

        public Int32 Page { get; set; }

        public Int32 PageSize { get; set; }

        public List<AdminUserDto> Users { get; set; }
    }

    public class BlockUserDto
    {
        public Int64 UserId { get; set; }

        public String Reason { get; set; }
    }

    public class UnblockUserDto
    {
        public Int64 UserId { get; set; }
    }

    public class NewAdminDto
    {
        public String Email { get; set; }

        public String Password { get; set; }
    }

    public class AdminAccountDto
    {
        public Int64 UserId { get; set; }

        public String Email { get; set; }

        public DateTime CreatedDate { get; set; }

        public Boolean IsLocked { get; set; }
    }

    public class AdminActionDto
    {
        public Int64 Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public String AdminEmail { get; set; }

        public String Action { get; set; }

        public String TargetEmail { get; set; }

        public String Detail { get; set; }
    }

    public class AdminActionPageDto
    {
        public Int32 Total { get; set; }

        public Int32 Page { get; set; }

        public Int32 PageSize { get; set; }

        public List<AdminActionDto> Actions { get; set; }
    }
}
