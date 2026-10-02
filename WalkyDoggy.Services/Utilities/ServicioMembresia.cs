using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
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
                contextoMembresia.Principal = new GenericPrincipal(identidad, new String[] { "Admin" });
            }

            return contextoMembresia;
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
