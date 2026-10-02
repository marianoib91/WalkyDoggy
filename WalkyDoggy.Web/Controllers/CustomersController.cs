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
using WalkyDoggy.Services;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/customers")]
    public class CustomersController : ControladorApiBase
    {
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;
        private readonly IServicioMembresia servicioMembresia;
        private readonly IServicioClientes servicioClientes;

        public CustomersController(IRepositorioEntidadBase<Customer> repositorioClientes,
                                 IServicioMembresia servicioMembresia,
                                 IServicioClientes servicioClientes,
                                 IRepositorioEntidadBase<Error> repositorioErrores,
                                 IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.repositorioClientes = repositorioClientes;
            this.servicioMembresia = servicioMembresia;
            this.servicioClientes = servicioClientes;
        }

        [HttpPost]
        [Route("register")]
        public HttpResponseMessage Register(HttpRequestMessage pedido, CustomerDto clienteDto)
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
                    if (servicioMembresia.ExisteUsuario(clienteDto.Email))
                    {
                        ModelState.AddModelError("E-mail invalido", "El email ingresado ya se encuentra en uso.");
                        respuesta = pedido.CreateResponse(HttpStatusCode.BadRequest,
                        ModelState.Keys.SelectMany(k => ModelState[k].Errors)
                              .Select(m => m.ErrorMessage).ToArray());
                    }
                    else
                    {
                        var cliente = this.servicioClientes.Registrar(clienteDto);
                        respuesta = pedido.CreateResponse<CustomerDto>(HttpStatusCode.OK, cliente);
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
                var clienteDto = this.servicioClientes.ObtenerPorIdUsuario(userId);

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, clienteDto);

                return respuesta;
            });
        }

        [HttpPost]
        [Route("update")]
        public HttpResponseMessage Update(HttpRequestMessage pedido, CustomerDto clienteDto)
        {
            return CrearRespuestaHttp(pedido, () =>
            {

                HttpResponseMessage respuesta = null;
                this.servicioClientes.Actualizar(clienteDto);

                respuesta = pedido.CreateResponse(HttpStatusCode.OK, clienteDto);

                return respuesta;
            });
        }
    }
}