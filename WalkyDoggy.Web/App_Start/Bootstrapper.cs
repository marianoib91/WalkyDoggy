using WalkyDoggy.Web.Mappings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace WalkyDoggy.Web.App_Start
{
    public class Bootstrapper
    {
        public static void Ejecutar()
        {
            // Configure Autofac
            AutofacWebapiConfig.Inicializar(GlobalConfiguration.Configuration);
            //Configure AutoMapper
            AutoMapperConfiguration.Configurar();
        }
    }
}