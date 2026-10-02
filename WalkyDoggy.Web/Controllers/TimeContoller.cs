using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    //TODO: Ver porque no anda este controller
    [RoutePrefix("api/times")]
    public class TimeContoller : ControladorApiBase
    {
        private readonly IServicioHorarios servicioHorarios;

        public TimeContoller(IServicioHorarios servicioHorarios,
                             IRepositorioEntidadBase<Error> repositorioErrores,
                             IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.servicioHorarios = servicioHorarios;
        }

        [HttpGet]
        [Route("getAvailableWalkTimes")]
        public HttpResponseMessage ObtenerHorariosDePaseoDisponibles(HttpRequestMessage pedido)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage respuesta = null;
                var horariosDto = this.servicioHorarios.ObtenerHorariosDePaseoDisponibles();
                respuesta = pedido.CreateResponse(HttpStatusCode.OK, horariosDto);

                return respuesta;
            });
        }

        [HttpGet]
        [Route("getAllWalkTimes")]
        public HttpResponseMessage ObtenerTodosLosHorariosDePaseo(HttpRequestMessage pedido)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage respuesta = null;
                var horariosDto = this.servicioHorarios.ObtenerTodosLosHorariosDePaseo();
                respuesta = pedido.CreateResponse(HttpStatusCode.OK, horariosDto);

                return respuesta;
            });
        }
    }
}