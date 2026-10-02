using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Entities.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/workDays")]
    public class WorkDaysController : ControladorApiBase
    {
        private readonly IServicioJornadas servicioJornadas;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;

        public WorkDaysController(IServicioJornadas servicioJornadas,
                                 IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                 IRepositorioEntidadBase<Error> repositorioErrores,
                                 IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.servicioJornadas = servicioJornadas;
            this.repositorioPaseadores = repositorioPaseadores;
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage pedido)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage respuesta = null;
                var jornadasDto = this.servicioJornadas.ObtenerTodos();
                respuesta = pedido.CreateResponse(HttpStatusCode.OK, jornadasDto);
                return respuesta;
            });
        }

        [HttpGet]
        [Route("getAllByWalkerId")]
        public HttpResponseMessage GetAllByWalkerId(HttpRequestMessage pedido, Int64 walkerId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage respuesta = null;
                var jornadasDto = this.servicioJornadas.ObtenerTodosPorIdPaseador(walkerId);
                respuesta = pedido.CreateResponse(HttpStatusCode.OK, jornadasDto);
                return respuesta;
            });
        }

        [HttpGet]
        [Route("getById")]
        public HttpResponseMessage GetById(HttpRequestMessage pedido, Int64 id)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage respuesta = null;
                var jornadasDto = this.servicioJornadas.ObtenerPorId(id);
                respuesta = pedido.CreateResponse(HttpStatusCode.OK, jornadasDto);
                return respuesta;
            });
        }

        //Guarda de una vez toda la semana de trabajo del paseador (cualquier dia, de 00:00 a 24:00). Solo el propio paseador.
        [HttpPost]
        [Route("saveWeek")]
        public HttpResponseMessage SaveWeek(HttpRequestMessage pedido, WorkWeekDto week)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                if (week == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "Faltan datos de la semana de trabajo." });
                }
                if (!IdentidadDelActor.EstaAutenticado(User))
                {
                    return pedido.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
                }
                if (!IdentidadDelActor.EsPaseador(User, repositorioPaseadores, week.WalkerId))
                {
                    return pedido.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés cambiar los horarios de otro paseador." });
                }

                String error;
                if (!this.servicioJornadas.SaveWeek(week.WalkerId, week.Ranges, out error))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                return pedido.CreateResponse(HttpStatusCode.OK, this.servicioJornadas.ObtenerTodosPorIdPaseador(week.WalkerId));
            });
        }

        [HttpPost]
        [Route("save")]
        public HttpResponseMessage Save(HttpRequestMessage pedido, WorkDayDto jornadaDto)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage respuesta = null;

                if (!ModelState.IsValid)
                {
                    respuesta = pedido.CreateResponse(HttpStatusCode.BadRequest,
                        ModelState.Keys.SelectMany(k => ModelState[k].Errors)
                              .Select(m => m.ErrorMessage).ToArray());
                }
                else
                {
                    var jornada = this.servicioJornadas.Guardar(jornadaDto);
                    if (jornada != null)
                    {
                        respuesta = pedido.CreateResponse<WorkDay>(HttpStatusCode.OK, jornada);
                    }
                    else
                    {
                        respuesta = pedido.CreateResponse(HttpStatusCode.InternalServerError, "Ya existe una jornada laboral con los valores seleccionados.");
                    }

                }

                return respuesta;
            });
        }

        public HttpResponseMessage Delete(HttpRequestMessage pedido, Int64 id)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage respuesta = null;
                this.servicioJornadas.Eliminar(id);
                respuesta = pedido.CreateResponse(HttpStatusCode.OK);
                return respuesta;
            });
        }
    }
}