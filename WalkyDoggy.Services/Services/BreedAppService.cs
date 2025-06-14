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
    public class BreedAppService : EntityBaseAppService<Breed, BreedDto>, IBreedAppService
    {
        #region Variables
        private readonly IEntityBaseRepository<Breed> breedsRepository;
        private readonly IEncryptionService encryptionService;
        private readonly IMembershipService membershipService;
        #endregion

        public BreedAppService(IEntityBaseRepository<Error> errorsRepository,
                                  IUnitOfWork unitOfWork,
                                  IEntityBaseRepository<Breed> breedsRepository,
                                  IEncryptionService encryptionService,
                                  IMembershipService membershipService) :
            base(errorsRepository, unitOfWork, breedsRepository)
        {
            this.breedsRepository = breedsRepository;
            this.encryptionService = encryptionService;
            this.membershipService = membershipService;
        }

        public List<BreedDto> GetAll()
        {
            var breeds = this.entityRepository.GetAll().ToList();
            var breedsDto = Mapper.Map<List<Breed>, List<BreedDto>>(breeds);
            return breedsDto;
        }
    }
}
