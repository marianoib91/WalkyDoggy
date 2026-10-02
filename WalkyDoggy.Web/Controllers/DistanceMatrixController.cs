using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    public class DistanceMatrixController : ControladorApiBase
    {
        private readonly IServicioMatrizDistancias servicioMatrizDistancias;

        public DistanceMatrixController(IServicioMatrizDistancias servicioMatrizDistancias,
                                        IRepositorioEntidadBase<Error> repositorioErrores,
                                        IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.servicioMatrizDistancias = servicioMatrizDistancias;
        }
    }
}