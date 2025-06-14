using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities;
using WalkyDoggy.Entities.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    public interface IEntityBaseAppService<E>
       where E : class, IEntityBase, new()
    {

    }

    public interface IEntityBaseAppService<E, D>
        where E : class, IEntityBase, new()
        where D : class, IDto, new()
    {

        IEnumerable<D> GetAll();

        D GetById(Int64 id);

        void Save(D entityDto);

        void DeleteById(Int64 id);
    }
}
