using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities.Dtos;

namespace WalkyDoggy.Services.Dtos
{
    public class UserDto:IDto
    {
        public Int64 Id { get; set; }

        public String Email { get; set; }

        public String Password { get; set; }

        public Boolean IsLocked { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
