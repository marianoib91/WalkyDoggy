using Autofac;
using Autofac.Core;
using Autofac.Integration.WebApi;
using WalkyDoggy.Data;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Services;
using WalkyDoggy.Web.Infrastructure.Core;
using WalkyDoggy.Web.Infrastructure.Email;
using WalkyDoggy.Web.Infrastructure.MercadoPago;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Services.Abstract;
using WalkyDoggy.Services.Services;
using FluentValidation;

namespace WalkyDoggy.Web.App_Start
{
    public class AutofacWebapiConfig
    {
        public static IContainer Container;
        public static void Inicializar(HttpConfiguration config)
        {
            Inicializar(config, RegistrarServicios(new ContainerBuilder()));
        }

        public static void Inicializar(HttpConfiguration config, IContainer container)
        {
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }

        private static IContainer RegistrarServicios(ContainerBuilder builder)
        {
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());


            builder.RegisterType<WalkyDoggyContext>()
                   .As<DbContext>()
                   .InstancePerRequest();

            builder.RegisterType<FabricaDb>()
                .As<IFabricaDb>()
                .InstancePerRequest();

            builder.RegisterType<UnidadDeTrabajo>()
                .As<IUnidadDeTrabajo>()
                .InstancePerRequest();

            builder.RegisterGeneric(typeof(RepositorioEntidadBase<>))
                   .As(typeof(IRepositorioEntidadBase<>))
                   .InstancePerRequest();

            // Services
            builder.RegisterType<ServicioEncriptacion>()
                .As<IServicioEncriptacion>()
                .InstancePerRequest();

            builder.RegisterType<ServicioMembresia>()
                .As<IServicioMembresia>()
                .InstancePerRequest();

            builder.RegisterType<ServicioUsuarios>()
                .As<IServicioUsuarios>()
                .InstancePerRequest();

            builder.RegisterType<ServicioClientes>()
               .As<IServicioClientes>()
               .InstancePerRequest();

            builder.RegisterType<ServicioPaseadores>()
               .As<IServicioPaseadores>()
               .InstancePerRequest();

            builder.RegisterType<ServicioMascotas>()
             .As<IServicioMascotas>()
             .InstancePerRequest();

            builder.RegisterType<ServicioRazas>()
             .As<IServicioRazas>()
             .InstancePerRequest();

            builder.RegisterType<ServicioTamanos>()
             .As<IServicioTamanos>()
             .InstancePerRequest();

            builder.RegisterType<ServicioProvincias>()
            .As<IServicioProvincias>()
            .InstancePerRequest();

            builder.RegisterType<ServicioCiudades>()
            .As<IServicioCiudades>()
            .InstancePerRequest();

            builder.RegisterType<ServicioPaseos>()
            .As<IServicioPaseos>()
            .InstancePerRequest();

            builder.RegisterType<EnviadorCorreosSmtp>()
            .As<IEnviadorCorreos>()
            .InstancePerRequest();

            builder.RegisterType<ServicioNotificaciones>()
            .As<IServicioNotificaciones>()
            .InstancePerRequest();

            builder.RegisterType<PasarelaDePagoMercadoPago>()
            .As<IPasarelaDePago>()
            .InstancePerRequest();

            builder.RegisterType<ProveedorTokensVendedor>()
            .As<IProveedorTokensVendedor>()
            .InstancePerRequest();

            builder.RegisterType<ServicioPagos>()
            .As<IServicioPagos>()
            .InstancePerRequest();

            builder.RegisterType<ServicioResenasMascotas>()
            .As<IServicioResenasMascotas>()
            .InstancePerRequest();

            builder.RegisterType<ServicioPublicidad>()
            .As<IServicioPublicidad>()
            .InstancePerRequest();

            builder.RegisterType<ServicioPaseosAdmin>()
            .As<IServicioPaseosAdmin>()
            .InstancePerRequest();

            builder.RegisterType<ServicioTablero>()
            .As<IServicioTablero>()
            .InstancePerRequest();

            builder.RegisterType<ServicioDenuncias>()
            .As<IServicioDenuncias>()
            .InstancePerRequest();

            builder.RegisterType<ServicioModeracion>()
            .As<IServicioModeracion>()
            .InstancePerRequest();

            builder.RegisterType<ServicioCaracteristicas>()
            .As<IServicioCaracteristicas>()
            .InstancePerRequest();

            builder.RegisterType<ServicioCatalogos>()
            .As<IServicioCatalogos>()
            .InstancePerRequest();

            builder.RegisterType<ServicioAdministracion>()
            .As<IServicioAdministracion>()
            .InstancePerRequest();

            builder.RegisterType<ServicioFavoritos>()
            .As<IServicioFavoritos>()
            .InstancePerRequest();

            builder.RegisterType<ServicioMensajes>()
            .As<IServicioMensajes>()
            .InstancePerRequest();

            builder.RegisterType<ServicioValoraciones>()
            .As<IServicioValoraciones>()
            .InstancePerRequest();

            builder.RegisterType<ServicioPrecios>()
            .As<IServicioPrecios>()
            .InstancePerRequest();

            builder.RegisterType<ServicioJornadas>()
            .As<IServicioJornadas>()
            .InstancePerRequest();

            builder.RegisterType<ServicioMatrizDistancias>()
          .As<IServicioMatrizDistancias>()
          .InstancePerRequest();

            builder.RegisterType<ServicioHorarios>()
             .As<IServicioHorarios>()
              .InstancePerRequest();

            // Generic Data Repository Factory
            builder.RegisterType<FabricaRepositorios>()
                .As<IFabricaRepositorios>().InstancePerRequest();

            #region Validacion     
            builder.RegisterType<AutoFacValidatorFactory>()
                .As<IValidatorFactory>()
                .InstancePerLifetimeScope();

            #endregion

            Container = builder.Build();

            return Container;
        }
    }
}
