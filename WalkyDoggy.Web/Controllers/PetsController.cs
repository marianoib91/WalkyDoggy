using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/pets")]
    public class PetsController : ControladorApiBase
    {
        private readonly IRepositorioEntidadBase<Pet> repositorioMascotas;
        private readonly IServicioMembresia servicioMembresia;
        private readonly IServicioMascotas servicioMascotas;
        private readonly IServicioCaracteristicas servicioCaracteristicas;
        private readonly IServicioCatalogos servicioCatalogos;

        public PetsController(IRepositorioEntidadBase<Pet> repositorioMascotas,
                                 IServicioMembresia servicioMembresia,
                                 IServicioMascotas servicioMascotas,
                                 IServicioCaracteristicas servicioCaracteristicas,
                                 IServicioCatalogos servicioCatalogos,
                                 IRepositorioEntidadBase<Error> repositorioErrores,
                                 IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.repositorioMascotas = repositorioMascotas;
            this.servicioMembresia = servicioMembresia;
            this.servicioMascotas = servicioMascotas;
            this.servicioCaracteristicas = servicioCaracteristicas;
            this.servicioCatalogos = servicioCatalogos;
        }

        //Controla las caracteristicas marcadas (existen, una sola por par), la raza y el tamaño (existen y no estan dados de baja)
        //y deja las caracteristicas ordenadas. Devuelve el motivo del error, o null si esta bien.
        private String ValidarCaracteristicas(PetDto mascotaDto)
        {
            String normalizadas;
            var error = this.servicioCaracteristicas.Validar(mascotaDto.Traits, out normalizadas);
            if (error == null)
            {
                mascotaDto.Traits = normalizadas;
                this.servicioCatalogos.RazaYTamanoDisponibles(mascotaDto.Id, mascotaDto.BreedId, mascotaDto.SizeId, out error);
            }
            return error;
        }

        [HttpPost]
        [Route("register")]
        public HttpResponseMessage Register(HttpRequestMessage pedido, PetDto mascotaDto)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage respuesta = null;
                String error;

                if (!ModelState.IsValid)
                {
                    respuesta = pedido.CreateResponse(HttpStatusCode.BadRequest,
                        ModelState.Keys.SelectMany(k => ModelState[k].Errors)
                              .Select(m => m.ErrorMessage).ToArray());
                }
                else if ((error = ValidarCaracteristicas(mascotaDto)) != null)
                {
                    respuesta = pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }
                else
                {
                    var mascota = this.servicioMascotas.Registrar(mascotaDto);
                    respuesta = pedido.CreateResponse<Pet>(HttpStatusCode.OK, mascota);

                }

                return respuesta;
            });
        }
        [HttpPost]
        [Route("update")]
        public HttpResponseMessage Update(HttpRequestMessage pedido, PetDto mascotaDto)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage respuesta = null;
                String error;

                if (!ModelState.IsValid)
                {
                    respuesta = pedido.CreateResponse(HttpStatusCode.BadRequest,
                        ModelState.Keys.SelectMany(k => ModelState[k].Errors)
                              .Select(m => m.ErrorMessage).ToArray());
                }
                else if ((error = ValidarCaracteristicas(mascotaDto)) != null)
                {
                    respuesta = pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }
                else
                {
                    var mascota = this.servicioMascotas.Actualizar(mascotaDto);
                    respuesta = pedido.CreateResponse<Pet>(HttpStatusCode.OK, mascota);

                }

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
                var mascotaDto = this.servicioMascotas.ObtenerPorId(id);

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, mascotaDto);

                return respuesta;
            });
        }


        [HttpGet]
        [Route("getAllByCustomerId")]
        public HttpResponseMessage GetAllByCustomerId(HttpRequestMessage pedido, Int64 customerId)
        {
            return CrearRespuestaHttp(pedido, () =>
            {

                HttpResponseMessage respuesta = null;
                var mascotasDto = this.servicioMascotas.ObtenerTodosPorIdCliente(customerId);

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, mascotasDto);

                return respuesta;
            });
        }

        public HttpResponseMessage Delete(HttpRequestMessage pedido, Int64 id)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage respuesta = null;
                var mascota = repositorioMascotas.ObtenerUno(id);
                repositorioMascotas.Eliminar(mascota);
                _unidadDeTrabajo.GuardarCambios();
                respuesta = pedido.CreateResponse(HttpStatusCode.OK, mascota);
                return respuesta;
            });
        }
    }
}