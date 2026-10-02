using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Entities.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    public class ServicioEntidadBase<E, D>
        where E : class, IEntityBase, new()
        where D : class, IDto, new()
    {

        protected readonly IRepositorioEntidadBase<Error> _repositorioErrores;

        protected readonly IUnidadDeTrabajo unidadDeTrabajo;

        protected readonly IRepositorioEntidadBase<E> repositorioEntidad;

        public ServicioEntidadBase(IRepositorioEntidadBase<Error> repositorioErrores,
                               IUnidadDeTrabajo unidadDeTrabajo)
        {
            _repositorioErrores = repositorioErrores;
            this.unidadDeTrabajo = unidadDeTrabajo;
        }

        public ServicioEntidadBase(IRepositorioEntidadBase<Error> repositorioErrores,
                                    IUnidadDeTrabajo unidadDeTrabajo,
                                    IRepositorioEntidadBase<E> repositorioEntidad)
        {
            _repositorioErrores = repositorioErrores;
            this.unidadDeTrabajo = unidadDeTrabajo;
            this.repositorioEntidad = repositorioEntidad;
        }

        public virtual IEnumerable<D> ObtenerTodos()
        {
            var entidades = this.repositorioEntidad.ObtenerTodos().ToList();
            var dtos = Mapper.Map<IEnumerable<E>,
                                   IEnumerable<D>>(entidades);

            return dtos;
        }

        public virtual D ObtenerPorId(Int64 id)
        {
            var entidad = this.repositorioEntidad.ObtenerUno(id);
            var dtos = Mapper.Map<E, D>(entidad);
            return dtos;
        }

        public virtual void EliminarPorId(Int64 id)
        {
            var entidad = this.repositorioEntidad.ObtenerUno(id);

            if (entidad != null)
            {
                this.repositorioEntidad.Eliminar(entidad);
                this.unidadDeTrabajo.GuardarCambios();
            }
        }

        public virtual void Guardar(D dto)
        {
            if (dto.Id == 0)
            {
                var entidad = Mapper.Map<D, E>(dto);
                this.repositorioEntidad.Agregar(entidad);
            }
            else
            {
                var entidad = Mapper.Map<D, E>(dto);
                this.repositorioEntidad.Editar(entidad);
            }
            this.unidadDeTrabajo.GuardarCambios();
        }
    }
}
