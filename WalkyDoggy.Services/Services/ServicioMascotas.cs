using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Services.Dtos;

namespace WalkyDoggy.Services.Services
{
    public class ServicioMascotas : ServicioEntidadBase<Pet, PetDto>, IServicioMascotas
    {
        #region Variables
        private readonly IRepositorioEntidadBase<Pet> repositorioMascotas;
        private readonly IServicioEncriptacion servicioEncriptacion;
        private readonly IServicioMembresia servicioMembresia;
        #endregion

        public ServicioMascotas(IRepositorioEntidadBase<Error> repositorioErrores,
                                  IUnidadDeTrabajo unidadDeTrabajo,
                                  IRepositorioEntidadBase<Pet> repositorioMascotas,
                                  IServicioEncriptacion servicioEncriptacion,
                                  IServicioMembresia servicioMembresia) :
            base(repositorioErrores, unidadDeTrabajo, repositorioMascotas)
        {
            this.repositorioMascotas = repositorioMascotas;
            this.servicioEncriptacion = servicioEncriptacion;
            this.servicioMembresia = servicioMembresia;
        }

        public Pet Registrar(PetDto mascotaDto)
        {
            //Se registra la mascota
            var mascota = Mapper.Map<PetDto, Pet>(mascotaDto);
            this.repositorioMascotas.Agregar(mascota);

            //Se guardan los registros en la base de datos y se devuelve la mascota nueva
            this.unidadDeTrabajo.GuardarCambios();

            return mascota;
        }

        public Pet Actualizar(PetDto mascotaDto)
        {
            var mascota = Mapper.Map<PetDto, Pet>(mascotaDto);
            this.repositorioMascotas.Editar(mascota);
            this.unidadDeTrabajo.GuardarCambios();

            return mascota;
        }

        public PetDto ObtenerPorId(Int64 id)
        {
            var mascota = this.repositorioMascotas.ObtenerTodos().Where(x => x.Id == id).FirstOrDefault();

            var mascotaDto = Mapper.Map<Pet, PetDto>(mascota);

            return mascotaDto;
        }

        public List<PetDto> ObtenerTodosPorIdCliente(Int64 idCliente)
        {
            var mascotas = this.repositorioMascotas.TodosConIncluidos(x => x.Breed, x => x.Size).
                                           Where(x => x.CustomerId == idCliente).
                                           ToList();

            var mascotasDto = Mapper.Map<List<Pet>, List<PetDto>>(mascotas);

            return mascotasDto;

        }
    }
}
