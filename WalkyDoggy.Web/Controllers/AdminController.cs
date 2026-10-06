using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    //Funciones del administrador sobre usuarios, administradores y bitacora (ver ControladorAdminBase: todas exigen ser administrador)
    [RoutePrefix("api/admin")]
    public class AdminController : ControladorAdminBase
    {
        private readonly IServicioAdministracion servicioAdministracion;

        public AdminController(IServicioAdministracion servicioAdministracion,
                               IRepositorioEntidadBase<User> repositorioUsuarios,
                               IRepositorioEntidadBase<Error> repositorioErrores,
                               IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioUsuarios, repositorioErrores, unidadDeTrabajo)
        {
            this.servicioAdministracion = servicioAdministracion;
        }

        [HttpGet]
        [Route("users")]
        public HttpResponseMessage Users(HttpRequestMessage pedido, String role = null, String status = null, String search = null, Int32 page = 1, Int32 pageSize = 15)
        {
            return ComoAdministrador(pedido, idAdministrador =>
                pedido.CreateResponse(HttpStatusCode.OK, servicioAdministracion.ListarUsuarios(role, status, search, page, pageSize)));
        }

        [HttpPost]
        [Route("users/block")]
        public HttpResponseMessage Block(HttpRequestMessage pedido, BlockUserDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioAdministracion.Bloquear(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        [HttpPost]
        [Route("users/unblock")]
        public HttpResponseMessage Unblock(HttpRequestMessage pedido, UnblockUserDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioAdministracion.Desbloquear(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        [HttpGet]
        [Route("admins")]
        public HttpResponseMessage Admins(HttpRequestMessage pedido)
        {
            return ComoAdministrador(pedido, idAdministrador =>
                pedido.CreateResponse(HttpStatusCode.OK, servicioAdministracion.ListarAdministradores()));
        }

        [HttpPost]
        [Route("admins")]
        public HttpResponseMessage CreateAdmin(HttpRequestMessage pedido, NewAdminDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioAdministracion.CrearAdministrador(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        [HttpGet]
        [Route("log")]
        public HttpResponseMessage Log(HttpRequestMessage pedido, Int32 page = 1, Int32 pageSize = 20)
        {
            return ComoAdministrador(pedido, idAdministrador =>
                pedido.CreateResponse(HttpStatusCode.OK, servicioAdministracion.ListarBitacora(page, pageSize)));
        }
    }
}
