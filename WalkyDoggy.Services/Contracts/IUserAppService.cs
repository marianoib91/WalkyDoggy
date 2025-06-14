using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Services.Dtos;

namespace WalkyDoggy.Services.Abstract
{
    public interface IUserAppService:IEntityBaseAppService<User,UserDto>
    {
        UserDto GetByEmail(String email);
        Int64 GetRole(Boolean isWalker);
    }
}
