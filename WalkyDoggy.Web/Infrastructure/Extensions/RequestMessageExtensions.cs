using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http.Dependencies;

namespace WalkyDoggy.Web.Infrastructure.Extensions
{
    public static class RequestMessageExtensions
    {
        internal static IServicioMembresia ObtenerServicioMembresia(this HttpRequestMessage pedido)
        {
            return pedido.ObtenerServicio<IServicioMembresia>();
        }

        internal static IRepositorioEntidadBase<T> ObtenerRepositorioDeDatos<T>(this HttpRequestMessage pedido) where T : class, IEntityBase, new()
        {
            return pedido.ObtenerServicio<IRepositorioEntidadBase<T>>();
        }

        private static TService ObtenerServicio<TService>(this HttpRequestMessage pedido)
        {
            IDependencyScope ambitoDeDependencias = pedido.GetDependencyScope();
            TService servicio = (TService)ambitoDeDependencias.GetService(typeof(TService));

            return servicio;
        }
    }
}