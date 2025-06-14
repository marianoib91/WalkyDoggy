using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Entities.Contracts
{
    public interface IUser:IEntityBase
    {
        String Email { get; set; }
    }
}
