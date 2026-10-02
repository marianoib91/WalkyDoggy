using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Web.App_Start;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Autofac;
using System.Web.Http;
using WalkyDoggy.Web.Infrastructure.Extensions;
using System.Net.Http;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Web.Infrastructure.Core
{
    public class FabricaRepositorios : IFabricaRepositorios
    {
        public IRepositorioEntidadBase<T> ObtenerRepositorioDeDatos<T>(HttpRequestMessage pedido) where T : class, IEntityBase, new()
        {
            return pedido.ObtenerRepositorioDeDatos<T>();
        }
    }

    public interface IFabricaRepositorios
    {
        IRepositorioEntidadBase<T> ObtenerRepositorioDeDatos<T>(HttpRequestMessage pedido) where T : class, IEntityBase, new();
    }
}