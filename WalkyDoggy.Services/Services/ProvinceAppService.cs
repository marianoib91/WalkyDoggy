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
    public class ProvinceAppService : EntityBaseAppService<Province, ProvinceDto>, IProvinceAppService
    {
        #region Variables
        private readonly IEntityBaseRepository<Province> provincesRepository;
        private readonly IEncryptionService encryptionService;
        private readonly IMembershipService membershipService;
        #endregion

        public ProvinceAppService(IEntityBaseRepository<Error> errorsRepository,
                                  IUnitOfWork unitOfWork,
                                  IEntityBaseRepository<Province> provincesRepository,
                                  IEncryptionService encryptionService,
                                  IMembershipService membershipService) :
            base(errorsRepository, unitOfWork, provincesRepository)
        {
            this.provincesRepository = provincesRepository;
            this.encryptionService = encryptionService;
            this.membershipService = membershipService;
        }

        public List<ProvinceDto> GetAll()
        {
            var provinces = this.entityRepository.GetAll().ToList();
            var provincesDto = Mapper.Map<List<Province>, List<ProvinceDto>>(provinces);
            return provincesDto;
        }
    }
}
