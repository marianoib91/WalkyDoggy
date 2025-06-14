using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using System.Linq;
using System;

namespace WalkyDoggy.Data.Extensions
{
    public static class UserExtensions
    {

        public static bool UserExists(this IEntityBaseRepository<User> usersRepository, string email)
        {
            bool userExists = false;

            userExists = usersRepository.GetAll()
                .Any(c => c.Email.ToLower() == email);

            return userExists;
        }

        public static User GetSingleByEmail(this IEntityBaseRepository<User> userRepository, String email)
        {
            return userRepository.GetAll().FirstOrDefault(x => x.Email == email);
        }

        /* public static string GetUserFullName(this IEntityBaseRepository<User> usersRepository, int userId)
         {
             string userFullName = string.Empty;

             var user = usersRepository.GetSingle(userId);

             userFullName = user.FirstName + " " + user.LastName;

             return userFullName;
         }*/
    }
}
