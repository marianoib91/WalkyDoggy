using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities;
using WalkyDoggy.Entities.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    public interface IServicioEntidadBase<E>
       where E : class, IEntityBase, new()
    {

    }

    public interface IServicioEntidadBase<E, D>
        where E : class, IEntityBase, new()
        where D : class, IDto, new()
    {

        IEnumerable<D> ObtenerTodos();

        D ObtenerPorId(Int64 id);

        void Guardar(D entidadDto);

        void EliminarPorId(Int64 id);
    }
}
