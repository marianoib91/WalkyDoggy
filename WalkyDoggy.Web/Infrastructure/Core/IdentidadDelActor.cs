using System;
using System.Linq;
using System.Security.Principal;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Web.Infrastructure.Core
{
    //Comprueba que quien llama a la API inicio sesion como el cliente o el paseador sobre el que quiere actuar.
    //Se usa en las acciones que mueven dinero o cambian el estado de un paseo.
    public static class IdentidadDelActor
    {
        public static Boolean EstaAutenticado(IPrincipal usuario)
        {
            return usuario != null && usuario.Identity != null && usuario.Identity.IsAuthenticated;
        }

        //Solo un usuario con el rol de administrador (y con la cuenta habilitada) puede usar las funciones de administracion
        public static Boolean EsAdministrador(IPrincipal usuario, IRepositorioEntidadBase<User> repositorioUsuarios, out Int64 idAdministrador)
        {
            idAdministrador = 0;
            if (!EstaAutenticado(usuario))
            {
                return false;
            }

            var email = usuario.Identity.Name;
            var administrador = repositorioUsuarios.TodosConIncluidos(x => x.UserRoles).
                FirstOrDefault(x => x.Email == email && !x.IsLocked && x.UserRoles.Any(r => r.RoleId == WalkyDoggy.Application.Constants.Roles.Admin));
            if (administrador == null)
            {
                return false;
            }

            idAdministrador = administrador.Id;
            return true;
        }

        public static Boolean EsCliente(IPrincipal usuario, IRepositorioEntidadBase<Customer> repositorioClientes, Int64 idCliente)
        {
            if (!EstaAutenticado(usuario))
            {
                return false;
            }

            var cliente = repositorioClientes.TodosConIncluidos(x => x.User).FirstOrDefault(x => x.Id == idCliente);
            return cliente != null && cliente.User != null &&
                   String.Equals(cliente.User.Email, usuario.Identity.Name, StringComparison.OrdinalIgnoreCase);
        }

        public static Boolean EsPaseador(IPrincipal usuario, IRepositorioEntidadBase<Walker> repositorioPaseadores, Int64 idPaseador)
        {
            if (!EstaAutenticado(usuario))
            {
                return false;
            }

            var paseador = repositorioPaseadores.TodosConIncluidos(x => x.User).FirstOrDefault(x => x.Id == idPaseador);
            return paseador != null && paseador.User != null &&
                   String.Equals(paseador.User.Email, usuario.Identity.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
