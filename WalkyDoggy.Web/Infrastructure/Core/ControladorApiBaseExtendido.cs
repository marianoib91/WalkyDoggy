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
    public class ControladorApiBaseExtendido : ApiController
    {
        protected List<Type> _repositoriosRequeridos;

        protected readonly IFabricaRepositorios _fabricaRepositorios;
        protected IRepositorioEntidadBase<Error> _repositorioErrores;
       /* protected IEntityBaseRepository<Movie> _moviesRepository;
        protected IEntityBaseRepository<Rental> _rentalsRepository;
        protected IEntityBaseRepository<Stock> _stocksRepository;
        protected IEntityBaseRepository<Customer> _customersRepository;*/
        protected IUnidadDeTrabajo _unidadDeTrabajo;

        private HttpRequestMessage PedidoHttp;

        public ControladorApiBaseExtendido(IFabricaRepositorios fabricaRepositorios, IUnidadDeTrabajo unidadDeTrabajo)
        {
            _fabricaRepositorios = fabricaRepositorios;
            _unidadDeTrabajo = unidadDeTrabajo;
        }

        protected HttpResponseMessage CrearRespuestaHttp(HttpRequestMessage pedido, List<Type> repositorios, Func<HttpResponseMessage> function)
        {
            HttpResponseMessage respuesta = null;

            try
            {
                PedidoHttp = pedido;
                InicializarRepositorios(repositorios);
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
        
        private void InicializarRepositorios(List<Type> entidades)
        {
            _repositorioErrores = _fabricaRepositorios.ObtenerRepositorioDeDatos<Error>(PedidoHttp);

           /* if (entities.Any(e => e.FullName == typeof(Movie).FullName))
            {
                _moviesRepository = _dataRepositoryFactory.GetDataRepository<Movie>(RequestMessage);
            }

            if (entities.Any(e => e.FullName == typeof(Rental).FullName))
            {
                _rentalsRepository = _dataRepositoryFactory.GetDataRepository<Rental>(RequestMessage);
            }

            if (entities.Any(e => e.FullName == typeof(Customer).FullName))
            {
                _customersRepository = _dataRepositoryFactory.GetDataRepository<Customer>(RequestMessage);
            }

            if (entities.Any(e => e.FullName == typeof(Stock).FullName))
            {
                _stocksRepository = _dataRepositoryFactory.GetDataRepository<Stock>(RequestMessage);
            }

            if (entities.Any(e => e.FullName == typeof(User).FullName))
            {
                _stocksRepository = _dataRepositoryFactory.GetDataRepository<Stock>(RequestMessage);
            }*/
        }

        private void RegistrarError(Exception ex)
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