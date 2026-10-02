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
    public interface IServicioMembresia
    {
        ContextoMembresia ValidarUsuario(String email, String contrasena);

        User CrearUsuario(UserDto usuarioDto);

        User ObtenerUsuario(Int64 idUsuario);

        Boolean ExisteUsuario(String email);
    }
}
