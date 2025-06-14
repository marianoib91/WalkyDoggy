using Autofac;
using Autofac.Core;
using Autofac.Integration.WebApi;
using WalkyDoggy.Data;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Services;
using WalkyDoggy.Web.Infrastructure.Core;
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
        public static void Initialize(HttpConfiguration config)
        {
            Initialize(config, RegisterServices(new ContainerBuilder()));
        }

        public static void Initialize(HttpConfiguration config, IContainer container)
        {
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }

        private static IContainer RegisterServices(ContainerBuilder builder)
        {
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());


            builder.RegisterType<WalkyDoggyContext>()
                   .As<DbContext>()
                   .InstancePerRequest();

            builder.RegisterType<DbFactory>()
                .As<IDbFactory>()
                .InstancePerRequest();

            builder.RegisterType<UnitOfWork>()
                .As<IUnitOfWork>()
                .InstancePerRequest();

            builder.RegisterGeneric(typeof(EntityBaseRepository<>))
                   .As(typeof(IEntityBaseRepository<>))
                   .InstancePerRequest();

            // Services
            builder.RegisterType<EncryptionService>()
                .As<IEncryptionService>()
                .InstancePerRequest();

            builder.RegisterType<MembershipService>()
                .As<IMembershipService>()
                .InstancePerRequest();

            builder.RegisterType<UserAppService>()
                .As<IUserAppService>()
                .InstancePerRequest();

            builder.RegisterType<CustomerAppService>()
               .As<ICustomerAppService>()
               .InstancePerRequest();

            builder.RegisterType<WalkerAppService>()
               .As<IWalkerAppService>()
               .InstancePerRequest();

            builder.RegisterType<PetAppService>()
             .As<IPetAppService>()
             .InstancePerRequest();

            builder.RegisterType<BreedAppService>()
             .As<IBreedAppService>()
             .InstancePerRequest();

            builder.RegisterType<SizeAppService>()
             .As<ISizeAppService>()
             .InstancePerRequest();

            builder.RegisterType<ProvinceAppService>()
            .As<IProvinceAppService>()
            .InstancePerRequest();

            builder.RegisterType<CityAppService>()
            .As<ICityAppService>()
            .InstancePerRequest();

            builder.RegisterType<WalkAppService>()
            .As<IWalkAppService>()
            .InstancePerRequest();

            builder.RegisterType<PriceAppService>()
            .As<IPriceAppService>()
            .InstancePerRequest();

            builder.RegisterType<WorkDayAppService>()
            .As<IWorkDayAppService>()
            .InstancePerRequest();

            builder.RegisterType<GeocodeAppService>()
           .As<IGeocodeAppService>()
           .InstancePerRequest();

            builder.RegisterType<DistanceMatrixAppService>()
          .As<IDistanceMatrixAppService>()
          .InstancePerRequest();

            builder.RegisterType<TimeAppService>()
             .As<ITimeAppService>()
              .InstancePerRequest();

            // Generic Data Repository Factory
            builder.RegisterType<DataRepositoryFactory>()
                .As<IDataRepositoryFactory>().InstancePerRequest();

            #region Validation     
            builder.RegisterType<AutoFacValidatorFactory>()
                .As<IValidatorFactory>()
                .InstancePerLifetimeScope();

            #endregion

            Container = builder.Build();

            return Container;
        }
    }
}