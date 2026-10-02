using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using System.Linq;
using System;

namespace WalkyDoggy.Data.Extensions
{
    public static class UserExtensions
    {

        public static bool ExisteUsuario(this IRepositorioEntidadBase<User> repositorioUsuarios, string email)
        {
            bool existeUsuario = false;

            existeUsuario = repositorioUsuarios.ObtenerTodos()
                .Any(c => c.Email.ToLower() == email);

            return existeUsuario;
        }

        public static User ObtenerUnoPorEmail(this IRepositorioEntidadBase<User> repositorioUsuarios, String email)
        {
            return repositorioUsuarios.ObtenerTodos().FirstOrDefault(x => x.Email == email);
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
