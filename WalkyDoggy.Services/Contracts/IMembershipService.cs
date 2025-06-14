using WalkyDoggy.Entities;
using WalkyDoggy.Services.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities.Contracts;
using WalkyDoggy.Services.Dtos;

namespace WalkyDoggy.Services
{
    public interface IMembershipService
    {
        MembershipContext ValidateUser(String email, String password);

        User CreateUser(UserDto userDto);

        User GetUser(Int64 userId);

        Boolean UserExists(String email);
    }
}
