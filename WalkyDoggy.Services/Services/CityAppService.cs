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
    public class CityAppService : EntityBaseAppService<City, CityDto>, ICityAppService
    {
        #region Variables
        private readonly IEntityBaseRepository<City> citiesRepository;
        private readonly IEncryptionService encryptionService;
        private readonly IMembershipService membershipService;
        #endregion

        public CityAppService(IEntityBaseRepository<Error> errorsRepository,
                                  IUnitOfWork unitOfWork,
                                  IEntityBaseRepository<City> citiesRepository,
                                  IEncryptionService encryptionService,
                                  IMembershipService membershipService) :
            base(errorsRepository, unitOfWork, citiesRepository)
        {
            this.citiesRepository = citiesRepository;
            this.encryptionService = encryptionService;
            this.membershipService = membershipService;
        }

        public List<CityDto> GetAll()
        {
            var cities = this.entityRepository.GetAll().ToList();
            var citiesDto = Mapper.Map<List<City>, List<CityDto>>(cities);
            return citiesDto;
        }

        public List<CityDto> GetAllByProvinceId(Int64 provinceId)
        {
            var cities = this.entityRepository.GetAll().Where(x => x.ProvinceId == provinceId).ToList();
            var citiesDto = Mapper.Map<List<City>, List<CityDto>>(cities);
            return citiesDto;
        }
    }
}
