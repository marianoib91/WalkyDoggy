using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Application.Criterias;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Services.Services;
using WalkyDoggy.Services.Utilities;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/walkers")]
    public class WalkersController : ControladorApiBase
    {
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;
        private readonly IServicioMembresia servicioMembresia;
        private readonly IServicioPaseadores servicioPaseadores;

        public WalkersController(IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                 IServicioMembresia servicioMembresia,
                                 IServicioPaseadores servicioPaseadores,
                                 IRepositorioEntidadBase<Error> repositorioErrores,
                                 IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.repositorioPaseadores = repositorioPaseadores;
            this.servicioMembresia = servicioMembresia;
            this.servicioPaseadores = servicioPaseadores;
        }

        [HttpPost]
        [Route("register")]
        public HttpResponseMessage Register(HttpRequestMessage pedido, WalkerDto paseadorDto)
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
                else if (paseadorDto == null || paseadorDto.Amount < ServicioPaseadores.TarifaMinima || paseadorDto.Amount > ServicioPaseadores.TarifaMaxima)
                {
                    //Los validadores de FluentValidation no estan conectados a la API: la tarifa se valida aca
                    respuesta = pedido.CreateResponse(HttpStatusCode.BadRequest,
                        new[] { "Debe ingresar una tarifa por hora válida (entre $" + ServicioPaseadores.TarifaMinima + " y $" + ServicioPaseadores.TarifaMaxima.ToString("N0") + ")." });
                }
                else if (ServicioPaseadores.ValidarCondiciones(paseadorDto, true) != null)
                {
                    respuesta = pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { ServicioPaseadores.ValidarCondiciones(paseadorDto, true) });
                }
                else
                {
                    if (servicioMembresia.ExisteUsuario(paseadorDto.Email))
                    {
                        ModelState.AddModelError("E-mail invalido", "El email ingresado ya se encuentra en uso.");
                        respuesta = pedido.CreateResponse(HttpStatusCode.BadRequest,
                        ModelState.Keys.SelectMany(k => ModelState[k].Errors)
                              .Select(m => m.ErrorMessage).ToArray());
                    }
                    else
                    {
                        var paseador = this.servicioPaseadores.Registrar(paseadorDto);
                        respuesta = pedido.CreateResponse<WalkerDto>(HttpStatusCode.OK, paseador);
                    }
                }

                return respuesta;
            });
        }

        [HttpGet]
        [Route("getByUserId")]
        public HttpResponseMessage GetByUserId(HttpRequestMessage pedido, Int64 userId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {

                HttpResponseMessage respuesta = null;
                var paseadorDto = this.servicioPaseadores.ObtenerPorIdUsuario(userId);

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, paseadorDto);

                return respuesta;
            });
        }

        [HttpPost]
        [Route("update")]
        public HttpResponseMessage Update(HttpRequestMessage pedido, WalkerDto paseadorDto)
        {
            return CrearRespuestaHttp(pedido, () =>
            {

                if (paseadorDto != null && paseadorDto.Amount > 0 && (paseadorDto.Amount < ServicioPaseadores.TarifaMinima || paseadorDto.Amount > ServicioPaseadores.TarifaMaxima))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "La tarifa por hora tiene que estar entre $" + ServicioPaseadores.TarifaMinima + " y $" + ServicioPaseadores.TarifaMaxima.ToString("N0") + "." });
                }

                var errorZona = paseadorDto == null ? null : ServicioPaseadores.ValidarCondiciones(paseadorDto, false);
                if (errorZona != null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { errorZona });
                }

                HttpResponseMessage respuesta = null;
                this.servicioPaseadores.Actualizar(paseadorDto);

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, paseadorDto);

                return respuesta;
            });
        }

        //Paseadores que trabajan en la direccion de retiro (latitude y longitude con punto decimal), del mas cercano al mas lejano.
        //date (yyyy-MM-dd) y timeFrom (HH:00) son opcionales: filtran por los paseadores con horario libre.
        [HttpGet]
        [Route("getForPickup")]
        public HttpResponseMessage GetForPickup(HttpRequestMessage pedido, String latitude, String longitude, String date = null, String timeFrom = null)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                Double latitud, longitud;
                if (!Geografia.IntentarLeerCoordenadas(latitude, longitude, out latitud, out longitud))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "La dirección de retiro no tiene una ubicación válida." });
                }

                DateTime? fecha = null;
                if (!String.IsNullOrWhiteSpace(date))
                {
                    DateTime fechaLeida;
                    if (!DateTime.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out fechaLeida))
                    {
                        return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "La fecha no es válida." });
                    }
                    fecha = fechaLeida;
                }

                var paseadoresDto = this.servicioPaseadores.BuscarParaRetiro(latitud, longitud, fecha, timeFrom);

                return pedido.CreateResponse(HttpStatusCode.OK, paseadoresDto);
            });
        }

        [HttpGet]
        [Route("getDetail")]
        public HttpResponseMessage GetDetail(HttpRequestMessage pedido, Int64 id)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                var paseadorDto = this.servicioPaseadores.ObtenerDetalle(id);
                if (paseadorDto == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.NotFound, "El paseador no existe.");
                }

                return pedido.CreateResponse(HttpStatusCode.OK, paseadorDto);
            });
        }

        [HttpGet]
        [Route("getAvailableTimes")]
        public HttpResponseMessage GetAvailableTimes(HttpRequestMessage pedido, Int64 walkerId, String date)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                DateTime fechaLeida;
                if (!DateTime.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                                            System.Globalization.DateTimeStyles.None, out fechaLeida))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "La fecha no es válida." });
                }

                var horarios = this.servicioPaseadores.ObtenerHorariosDisponibles(walkerId, fechaLeida);

                return pedido.CreateResponse(HttpStatusCode.OK, horarios);
            });
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage pedido)
        {
            return CrearRespuestaHttp(pedido, () =>
            {

                HttpResponseMessage respuesta = null;

                var paseadoresDto = this.servicioPaseadores.ObtenerTodos();

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, paseadoresDto);

                return respuesta;
            });
        }
    }
}