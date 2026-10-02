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
    public class ServicioRazas : ServicioEntidadBase<Breed, BreedDto>, IServicioRazas
    {
        #region Variables
        private readonly IRepositorioEntidadBase<Breed> repositorioRazas;
        private readonly IServicioEncriptacion servicioEncriptacion;
        private readonly IServicioMembresia servicioMembresia;
        #endregion

        public ServicioRazas(IRepositorioEntidadBase<Error> repositorioErrores,
                                  IUnidadDeTrabajo unidadDeTrabajo,
                                  IRepositorioEntidadBase<Breed> repositorioRazas,
                                  IServicioEncriptacion servicioEncriptacion,
                                  IServicioMembresia servicioMembresia) :
            base(repositorioErrores, unidadDeTrabajo, repositorioRazas)
        {
            this.repositorioRazas = repositorioRazas;
            this.servicioEncriptacion = servicioEncriptacion;
            this.servicioMembresia = servicioMembresia;
        }

        public List<BreedDto> ObtenerTodos()
        {
            var razas = this.repositorioEntidad.ObtenerTodos().ToList();
            var razasDto = Mapper.Map<List<Breed>, List<BreedDto>>(razas);
            return razasDto;
        }
    }
}
