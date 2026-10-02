using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services;
using WalkyDoggy.Services.Utilities;
using WalkyDoggy.Web.Infrastructure.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Services.Abstract;
using WalkyDoggy.Services.Dtos;
using AutoMapper;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/account")]
    public class AccountController : ControladorApiBase
    {
        private readonly IRepositorioEntidadBase<User> repositorioUsuarios;
        private readonly IServicioMembresia servicioMembresia;
        private readonly IServicioUsuarios servicioUsuarios;

        public AccountController(IRepositorioEntidadBase<User> repositorioUsuarios,
                                 IServicioMembresia servicioMembresia,
                                 IServicioUsuarios servicioUsuarios,
                                 IRepositorioEntidadBase<Error> repositorioErrores,
                                 IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.repositorioUsuarios = repositorioUsuarios;
            this.servicioMembresia = servicioMembresia;
            this.servicioUsuarios = servicioUsuarios;
        }

        [AllowAnonymous]
        [Route("authenticate")]
        [HttpPost]
        public HttpResponseMessage Login(HttpRequestMessage pedido, LoginDto user)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage respuesta = null;

                if (ModelState.IsValid)
                {
                    ContextoMembresia contextoUsuario = servicioMembresia.ValidarUsuario(user.Email, user.Password);

                    if (contextoUsuario.User != null)
                    {
                        respuesta = pedido.CreateResponse(HttpStatusCode.OK, new
                        {
                            id = contextoUsuario.User.Id,
                            //En caso de tener mas de un roleId tenemos que implementar dos login, uno para los paseadores y otro para el cliente
                            roleId = contextoUsuario.User.UserRoles.Select(x => x.RoleId).FirstOrDefault(),
                            email = contextoUsuario.User.Email,
                            success = true
                        });
                    }
                    else
                    {
                        respuesta = pedido.CreateResponse(HttpStatusCode.OK, new { success = false });
                    }
                }
                else
                    respuesta = pedido.CreateResponse(HttpStatusCode.OK, new { success = false });

                return respuesta;
            });
        }

        [Route("register")]
        [HttpPost]
        public HttpResponseMessage Register(HttpRequestMessage pedido, UserDto usuarioDto)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage respuesta = null;

                if (!ModelState.IsValid)
                {
                    respuesta = pedido.CreateResponse(HttpStatusCode.BadRequest, new { success = false });
                }
                else
                {
                    var usuarioNuevo = servicioMembresia.CrearUsuario(usuarioDto);

                    if (usuarioNuevo != null)
                    {
                        respuesta = pedido.CreateResponse(HttpStatusCode.OK, new { success = true });
                    }
                    else
                    {
                        respuesta = pedido.CreateResponse(HttpStatusCode.OK, new { success = false });
                    }
                }

                return respuesta;
            });
        }
    }
}
