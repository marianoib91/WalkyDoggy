using System;

namespace WalkyDoggy.Entities
{
    //Bitacora: una accion que hizo un administrador (bloquear a alguien, crear otro administrador, etc.)
    public class AdminAction : IEntityBase
    {
        public Int64 Id { get; set; }

        public Int64 AdminUserId { get; set; }

        //Ver AdminActionTypes
        public String Action { get; set; }

        //Usuario sobre el que se hizo la accion, si corresponde
        public Int64? TargetUserId { get; set; }

        public String Detail { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
