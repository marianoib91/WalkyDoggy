using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Web.Infrastructure.Extensions;

namespace WalkyDoggy.Web.Infrastructure.Core
{
    public class ControladorApiBase : ApiController
    {
        protected readonly IRepositorioEntidadBase<Error> _repositorioErrores;
        protected readonly IUnidadDeTrabajo _unidadDeTrabajo;

        public ControladorApiBase(IRepositorioEntidadBase<Error> repositorioErrores, IUnidadDeTrabajo unidadDeTrabajo)
        {
            _repositorioErrores = repositorioErrores;
            _unidadDeTrabajo = unidadDeTrabajo;
        }

        public ControladorApiBase(IFabricaRepositorios fabricaRepositorios, IRepositorioEntidadBase<Error> repositorioErrores, IUnidadDeTrabajo unidadDeTrabajo)
        {
            _repositorioErrores = repositorioErrores;
            _unidadDeTrabajo = unidadDeTrabajo;
        }

        protected HttpResponseMessage CrearRespuestaHttp(HttpRequestMessage pedido, Func<HttpResponseMessage> function)
        {
            HttpResponseMessage respuesta = null;

            try
            {
                respuesta = function.Invoke();
            }
            catch (DbUpdateException ex)
            {
                RegistrarError(ex);
                respuesta = pedido.CreateResponse(HttpStatusCode.BadRequest, ex.InnerException.Message);
            }
            catch (Exception ex)
            {
                RegistrarError(ex);
                respuesta = pedido.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

            return respuesta;
        }
        protected void RegistrarError(Exception ex)
        {
            try
            {
                Error _error = new Error()
                {
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    DateCreated = DateTime.Now
                };

                _repositorioErrores.Agregar(_error);
                _unidadDeTrabajo.GuardarCambios();
            }
            catch { }
        }
    }
}