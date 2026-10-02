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
    public class ServicioTamanos : ServicioEntidadBase<Size, SizeDto>, IServicioTamanos
    {
        #region Variables
        private readonly IRepositorioEntidadBase<Size> repositorioTamanos;
        private readonly IServicioEncriptacion servicioEncriptacion;
        private readonly IServicioMembresia servicioMembresia;
        #endregion

        public ServicioTamanos(IRepositorioEntidadBase<Error> repositorioErrores,
                                  IUnidadDeTrabajo unidadDeTrabajo,
                                  IRepositorioEntidadBase<Size> repositorioTamanos,
                                  IServicioEncriptacion servicioEncriptacion,
                                  IServicioMembresia servicioMembresia) :
            base(repositorioErrores, unidadDeTrabajo, repositorioTamanos)
        {
            this.repositorioTamanos = repositorioTamanos;
            this.servicioEncriptacion = servicioEncriptacion;
            this.servicioMembresia = servicioMembresia;
        }

        public List<SizeDto> ObtenerTodos()
        {
            var tamanos = this.repositorioEntidad.ObtenerTodos().ToList();
            var tamanosDto = Mapper.Map<List<Size>, List<SizeDto>>(tamanos);
            return tamanosDto;
        }
    }
}
