using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Abstract;
using WalkyDoggy.Services.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    public class ServicioUsuarios : ServicioEntidadBase<User, UserDto>, IServicioUsuarios
    {
        #region Variables

        private readonly IServicioEncriptacion servicioEncriptacion;
        private readonly IServicioMembresia servicioMembresia;
        #endregion

        public ServicioUsuarios(IRepositorioEntidadBase<Error> repositorioErrores,
                              IUnidadDeTrabajo unidadDeTrabajo,
                              IRepositorioEntidadBase<User> repositorioUsuarios,
                              IServicioEncriptacion servicioEncriptacion,
                              IServicioMembresia servicioMembresia) :
            base(repositorioErrores, unidadDeTrabajo, repositorioUsuarios)
        {

            this.servicioEncriptacion = servicioEncriptacion;
            this.servicioMembresia = servicioMembresia;
        }

        public UserDto ObtenerPorEmail(String email)
        {
            var usuarioDto = this.ObtenerTodos().Where(x => x.Email == email).FirstOrDefault();
            return usuarioDto;
        }

        public Int64 ObtenerRol(Boolean esPaseador)
        {
            if (esPaseador)
            {
                return Roles.Walker;
            }
            return Roles.Customer;
        }
    }
}
