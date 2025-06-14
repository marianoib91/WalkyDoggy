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
    public class SizeAppService : EntityBaseAppService<Size, SizeDto>, ISizeAppService
    {
        #region Variables
        private readonly IEntityBaseRepository<Size> sizesRepository;
        private readonly IEncryptionService encryptionService;
        private readonly IMembershipService membershipService;
        #endregion

        public SizeAppService(IEntityBaseRepository<Error> errorsRepository,
                                  IUnitOfWork unitOfWork,
                                  IEntityBaseRepository<Size> sizesRepository,
                                  IEncryptionService encryptionService,
                                  IMembershipService membershipService) :
            base(errorsRepository, unitOfWork, sizesRepository)
        {
            this.sizesRepository = sizesRepository;
            this.encryptionService = encryptionService;
            this.membershipService = membershipService;
        }

        public List<SizeDto> GetAll()
        {
            var sizes = this.entityRepository.GetAll().ToList();
            var sizesDto = Mapper.Map<List<Size>, List<SizeDto>>(sizes);
            return sizesDto;
        }
    }
}
