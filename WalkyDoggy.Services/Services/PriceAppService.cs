using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;

namespace WalkyDoggy.Services.Services
{
    public class PriceAppService : EntityBaseAppService<Price, PriceDto>, IPriceAppService
    {
        #region Variables
        private readonly IEntityBaseRepository<Price> pricesRepository;
        private readonly IEncryptionService encryptionService;
        private readonly IMembershipService membershipService;
        #endregion

        public PriceAppService(IEntityBaseRepository<Error> errorsRepository,
                                  IUnitOfWork unitOfWork,
                                  IEntityBaseRepository<Price> pricesRepository,
                                  IEncryptionService encryptionService,
                                  IMembershipService membershipService) :
            base(errorsRepository, unitOfWork, pricesRepository)
        {
            this.pricesRepository = pricesRepository;
            this.encryptionService = encryptionService;
            this.membershipService = membershipService;
        }

        public List<PriceDto> GetAll()
        {
            var prices = this.entityRepository.GetAll().ToList();
            var pricesDto = Mapper.Map<List<Price>, List<PriceDto>>(prices);
            return pricesDto;
        }
    }
}

