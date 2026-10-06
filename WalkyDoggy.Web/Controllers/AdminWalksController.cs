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
using WalkyDoggy.Web.Infrastructure.MercadoPago;

namespace WalkyDoggy.Web.Controllers
{
    //El administrador ve todas las reservas, puede cancelar una en un caso excepcional y mira el tablero de metricas
    //(ver ControladorAdminBase: todas las acciones exigen ser administrador)
    [RoutePrefix("api/admin")]
    public class AdminWalksController : ControladorAdminBase
    {
        private readonly IServicioPaseosAdmin servicioPaseosAdmin;
        private readonly IServicioTablero servicioTablero;

        public AdminWalksController(IServicioPaseosAdmin servicioPaseosAdmin,
                                    IServicioTablero servicioTablero,
                                    IRepositorioEntidadBase<User> repositorioUsuarios,
                                    IRepositorioEntidadBase<Error> repositorioErrores,
                                    IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioUsuarios, repositorioErrores, unidadDeTrabajo)
        {
            this.servicioPaseosAdmin = servicioPaseosAdmin;
            this.servicioTablero = servicioTablero;
        }

        [HttpGet]
        [Route("walks")]
        public HttpResponseMessage Walks(HttpRequestMessage pedido, String status = null, String kind = null, [FromUri(Name = "from")] DateTime? desde = null, [FromUri(Name = "to")] DateTime? hasta = null, String search = null, Int32 page = 1, Int32 pageSize = 15)
        {
            return ComoAdministrador(pedido, idAdministrador =>
                pedido.CreateResponse(HttpStatusCode.OK, servicioPaseosAdmin.Listar(status, kind, desde, hasta, search, page, pageSize)));
        }

        [HttpPost]
        [Route("walks/cancel")]
        public HttpResponseMessage Cancel(HttpRequestMessage pedido, CancelWalkDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioPaseosAdmin.Cancelar(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        [HttpGet]
        [Route("dashboard")]
        public HttpResponseMessage Dashboard(HttpRequestMessage pedido, [FromUri(Name = "from")] DateTime? desde = null, [FromUri(Name = "to")] DateTime? hasta = null)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                var comision = ConfiguracionMercadoPago.DesdeConfiguracion().CommissionPercent;
                return pedido.CreateResponse(HttpStatusCode.OK, servicioTablero.Obtener(desde, hasta, comision));
            });
        }
    }
}
