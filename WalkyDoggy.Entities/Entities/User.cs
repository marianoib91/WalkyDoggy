using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities;
using WalkyDoggy.Entities.Contracts;
using WalkyDoggy.Entities.Entities;

namespace WalkyDoggy.Entities
{
    public class User : IEntityBase, IUser
    {
        public Int64 Id { get; set; }

        public String Email { get; set; }

        public String HashedPassword { get; set; }

        public String Salt { get; set; }

        public Boolean IsLocked { get; set; }

        //Por que un administrador bloqueo la cuenta y cuando (se le muestra a la persona al intentar entrar)
        public String BlockReason { get; set; }

        public DateTime? BlockedAt { get; set; }

        public DateTime CreatedDate { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; }

        public User()
        {
            UserRoles = new List<UserRole>();
        }

    }
}
