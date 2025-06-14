using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Entities
{
    public class UserRole : IEntityBase
    {
        public Int64 Id { get; set; }

        public Int64 UserId { get; set; }

        public Int64 RoleId { get; set; }

        public virtual Role Role { get; set; }

    }
}
