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
    public class PetAppService : EntityBaseAppService<Pet, PetDto>, IPetAppService
    {
        #region Variables
        private readonly IEntityBaseRepository<Pet> petsRepository;
        private readonly IEncryptionService encryptionService;
        private readonly IMembershipService membershipService;
        #endregion

        public PetAppService(IEntityBaseRepository<Error> errorsRepository,
                                  IUnitOfWork unitOfWork,
                                  IEntityBaseRepository<Pet> petsRepository,
                                  IEncryptionService encryptionService,
                                  IMembershipService membershipService) :
            base(errorsRepository, unitOfWork, petsRepository)
        {
            this.petsRepository = petsRepository;
            this.encryptionService = encryptionService;
            this.membershipService = membershipService;
        }

        public Pet Register(PetDto petDto)
        {
            //Se registra la mascota
            var pet = Mapper.Map<PetDto, Pet>(petDto);
            this.petsRepository.Add(pet);

            //Se guardan los registros en la base de datos y se devuelve la mascota nueva
            this.unitOfWork.Commit();

            return pet;
        }

        public Pet Update(PetDto petDto)
        {
            var pet = Mapper.Map<PetDto, Pet>(petDto);
            this.petsRepository.Edit(pet);
            this.unitOfWork.Commit();

            return pet;
        }

        public PetDto GetById(Int64 id)
        {
            var pet = this.petsRepository.GetAll().Where(x => x.Id == id).FirstOrDefault();

            var petDto = Mapper.Map<Pet, PetDto>(pet);

            return petDto;
        }

        public List<PetDto> GetAllByCustomerId(Int64 customerId)
        {
            var pets = this.petsRepository.AllIncluding(x => x.Breed, x => x.Size).
                                           Where(x => x.CustomerId == customerId).
                                           ToList();

            var petsDto = Mapper.Map<List<Pet>, List<PetDto>>(pets);

            return petsDto;

        }
    }
}
