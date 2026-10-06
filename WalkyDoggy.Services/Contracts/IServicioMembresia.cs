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

        //Las credenciales son correctas pero un administrador bloqueó la cuenta: devuelve el motivo para mostrárselo a la persona
        Boolean EstaBloqueado(String email, String contrasena, out String motivo);

        //Cambia la contraseña si la actual es correcta; si no, devuelve false y el motivo en error
        Boolean CambiarContrasena(String email, String contrasenaActual, String contrasenaNueva, out String error);
    }
}
