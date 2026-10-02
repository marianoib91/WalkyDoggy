using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;

namespace WalkyDoggy.Services.Services
{
    public class ServicioProvincias : ServicioEntidadBase<Province, ProvinceDto>, IServicioProvincias
    {
        #region Variables
        private readonly IRepositorioEntidadBase<Province> repositorioProvincias;
        private readonly IServicioEncriptacion servicioEncriptacion;
        private readonly IServicioMembresia servicioMembresia;
        #endregion

        public ServicioProvincias(IRepositorioEntidadBase<Error> repositorioErrores,
                                  IUnidadDeTrabajo unidadDeTrabajo,
                                  IRepositorioEntidadBase<Province> repositorioProvincias,
                                  IServicioEncriptacion servicioEncriptacion,
                                  IServicioMembresia servicioMembresia) :
            base(repositorioErrores, unidadDeTrabajo, repositorioProvincias)
        {
            this.repositorioProvincias = repositorioProvincias;
            this.servicioEncriptacion = servicioEncriptacion;
            this.servicioMembresia = servicioMembresia;
        }

        public List<ProvinceDto> ObtenerTodos()
        {
            var provincias = this.repositorioEntidad.ObtenerTodos().ToList();
            var provinciasDto = Mapper.Map<List<Province>, List<ProvinceDto>>(provincias);
            return provinciasDto;
        }
    }
}
