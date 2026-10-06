using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Entities.Contracts;
using WalkyDoggy.Services.Dtos;
using WalkyDoggy.Services.Utilities;

namespace WalkyDoggy.Services
{
    public class ServicioMembresia : IServicioMembresia
    {
        #region Variables
        private readonly IRepositorioEntidadBase<User> repositorioUsuarios;
        private readonly IServicioEncriptacion servicioEncriptacion;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;
        #endregion
        public ServicioMembresia(IRepositorioEntidadBase<User> repositorioUsuarios,
                                 IServicioEncriptacion servicioEncriptacion,
                                 IUnidadDeTrabajo unidadDeTrabajo)
        {
            this.repositorioUsuarios = repositorioUsuarios;
            this.servicioEncriptacion = servicioEncriptacion;
            this.unidadDeTrabajo = unidadDeTrabajo;
        }

        #region Implementacion de IServicioMembresia

        public ContextoMembresia ValidarUsuario(String email, String contrasena)
        {
            var contextoMembresia = new ContextoMembresia();
            //var user = userRepository.GetAll().Where(x => x.Email == email).FirstOrDefault();
            var usuario = repositorioUsuarios.TodosConIncluidos(x => x.UserRoles).Where(x => x.Email == email).FirstOrDefault();
            if (usuario != null && esUsuarioValido(usuario, contrasena))
            {
                contextoMembresia.User = usuario;
                var identidad = new GenericIdentity(email);
                //Antes todos los usuarios figuraban con el rol "Admin"; ahora el principal lleva los roles reales
                var roles = usuario.UserRoles.Select(x => x.RoleId == Roles.Admin ? "Admin" : x.RoleId == Roles.Walker ? "Walker" : "Customer").ToArray();
                contextoMembresia.Principal = new GenericPrincipal(identidad, roles);
            }

            return contextoMembresia;
        }
        public Boolean EstaBloqueado(String email, String contrasena, out String motivo)
        {
            motivo = null;
            var usuario = repositorioUsuarios.ObtenerTodos().Where(x => x.Email == email).FirstOrDefault();

            if (usuario == null || !usuario.IsLocked || !esContrasenaValida(usuario, contrasena))
            {
                return false;
            }

            motivo = usuario.BlockReason;
            return true;
        }

        public User CrearUsuario(UserDto usuarioDto)
        {
            /* var existingUser = userRepository.GetAll().Where(x => x.Email == userDto.Email).FirstOrDefault();

             if (existingUser != null)
             {
                 throw new Exception("El email ingresado ya se encuentra en uso.");
             }*/

            var salContrasena = servicioEncriptacion.CrearSal();

            var usuarioNuevo = Mapper.Map<UserDto, User>(usuarioDto);
            usuarioNuevo.Salt = salContrasena;
            usuarioNuevo.IsLocked = false;
            usuarioNuevo.HashedPassword = servicioEncriptacion.EncriptarContrasena(usuarioDto.Password, salContrasena);
            usuarioNuevo.CreatedDate = DateTime.Now;

            repositorioUsuarios.Agregar(usuarioNuevo);

            unidadDeTrabajo.GuardarCambios();

            return usuarioNuevo;
        }

        public User ObtenerUsuario(Int64 idUsuario)
        {
            return repositorioUsuarios.ObtenerUno(idUsuario);
        }

        public Boolean ExisteUsuario(String email)
        {
            var usuarioExistente = repositorioUsuarios.ObtenerTodos().
                                            Where(x => x.Email == email).
                                            FirstOrDefault();
            if (usuarioExistente != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CambiarContrasena(String email, String contrasenaActual, String contrasenaNueva, out String error)
        {
            error = null;
            var usuario = repositorioUsuarios.ObtenerTodos().FirstOrDefault(x => x.Email == email);

            if (usuario == null || String.IsNullOrEmpty(contrasenaActual) || !esUsuarioValido(usuario, contrasenaActual))
            {
                error = "La contraseña actual no es correcta.";
                return false;
            }

            if (String.IsNullOrEmpty(contrasenaNueva) || contrasenaNueva.Length < 6 || contrasenaNueva.Length > 50)
            {
                error = "La contraseña nueva debe tener entre 6 y 50 caracteres.";
                return false;
            }

            if (contrasenaNueva == contrasenaActual)
            {
                error = "La contraseña nueva tiene que ser distinta de la actual.";
                return false;
            }

            //Se genera una sal nueva para que el hash no se repita
            usuario.Salt = servicioEncriptacion.CrearSal();
            usuario.HashedPassword = servicioEncriptacion.EncriptarContrasena(contrasenaNueva, usuario.Salt);
            repositorioUsuarios.Editar(usuario);
            unidadDeTrabajo.GuardarCambios();

            return true;
        }
        #endregion

        #region Metodos auxiliares

        private bool esContrasenaValida(User usuario, String contrasena)
        {
            return String.Equals(servicioEncriptacion.EncriptarContrasena(contrasena, usuario.Salt), usuario.HashedPassword);
        }

        private bool esUsuarioValido(User usuario, String contrasena)
        {
            if (esContrasenaValida(usuario, contrasena))
            {
                return !usuario.IsLocked;
            }

            return false;
        }
        #endregion
    }
}
