using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;

namespace WalkyDoggy.Services.Services
{
    public class ServicioPrecios : ServicioEntidadBase<Price, PriceDto>, IServicioPrecios
    {
        #region Variables
        private readonly IRepositorioEntidadBase<Price> repositorioPrecios;
        private readonly IServicioEncriptacion servicioEncriptacion;
        private readonly IServicioMembresia servicioMembresia;
        #endregion

        public ServicioPrecios(IRepositorioEntidadBase<Error> repositorioErrores,
                                  IUnidadDeTrabajo unidadDeTrabajo,
                                  IRepositorioEntidadBase<Price> repositorioPrecios,
                                  IServicioEncriptacion servicioEncriptacion,
                                  IServicioMembresia servicioMembresia) :
            base(repositorioErrores, unidadDeTrabajo, repositorioPrecios)
        {
            this.repositorioPrecios = repositorioPrecios;
            this.servicioEncriptacion = servicioEncriptacion;
            this.servicioMembresia = servicioMembresia;
        }

        public List<PriceDto> ObtenerTodos()
        {
            var precios = this.repositorioEntidad.ObtenerTodos().ToList();
            var preciosDto = Mapper.Map<List<Price>, List<PriceDto>>(precios);
            return preciosDto;
        }
    }
}

