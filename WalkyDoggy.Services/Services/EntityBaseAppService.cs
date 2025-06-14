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
    public class EntityBaseAppService<E, D>
        where E : class, IEntityBase, new()
        where D : class, IDto, new()
    {

        protected readonly IEntityBaseRepository<Error> _errorsRepository;

        protected readonly IUnitOfWork unitOfWork;

        protected readonly IEntityBaseRepository<E> entityRepository;

        public EntityBaseAppService(IEntityBaseRepository<Error> errorsRepository,
                               IUnitOfWork unitOfWork)
        {
            _errorsRepository = errorsRepository;
            this.unitOfWork = unitOfWork;
        }

        public EntityBaseAppService(IEntityBaseRepository<Error> errorsRepository,
                                    IUnitOfWork unitOfWork,
                                    IEntityBaseRepository<E> entityRepository)
        {
            _errorsRepository = errorsRepository;
            this.unitOfWork = unitOfWork;
            this.entityRepository = entityRepository;
        }

        public virtual IEnumerable<D> GetAll()
        {
            var entities = this.entityRepository.GetAll().ToList();
            var dtos = Mapper.Map<IEnumerable<E>,
                                   IEnumerable<D>>(entities);

            return dtos;
        }

        public virtual D GetById(Int64 id)
        {
            var area = this.entityRepository.GetSingle(id);
            var dtos = Mapper.Map<E, D>(area);
            return dtos;
        }

        public virtual void DeleteById(Int64 id)
        {
            var area = this.entityRepository.GetSingle(id);

            if (area != null)
            {
                this.entityRepository.Delete(area);
                this.unitOfWork.Commit();
            }
        }

        public virtual void Save(D dto)
        {
            if (dto.Id == 0)
            {
                var entity = Mapper.Map<D, E>(dto);
                this.entityRepository.Add(entity);
            }
            else
            {
                var entity = Mapper.Map<D, E>(dto);
                this.entityRepository.Edit(entity);
            }
            this.unitOfWork.Commit();
        }
    }
}
