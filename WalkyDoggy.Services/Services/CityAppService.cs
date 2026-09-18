using AutoMapper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Criterias;
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
        private readonly IEntityBaseRepository<Province> provincesRepository;
        private readonly IEncryptionService encryptionService;
        private readonly IMembershipService membershipService;
        #endregion

        public CityAppService(IEntityBaseRepository<Error> errorsRepository,
                                  IUnitOfWork unitOfWork,
                                  IEntityBaseRepository<City> citiesRepository,
                                  IEntityBaseRepository<Province> provincesRepository,
                                  IEncryptionService encryptionService,
                                  IMembershipService membershipService) :
            base(errorsRepository, unitOfWork, citiesRepository)
        {
            this.citiesRepository = citiesRepository;
            this.provincesRepository = provincesRepository;
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

        public CityDto Resolve(CityResolveCriteria cityResolveCriteria)
        {
            if (cityResolveCriteria == null ||
                String.IsNullOrWhiteSpace(cityResolveCriteria.CityName) ||
                String.IsNullOrWhiteSpace(cityResolveCriteria.ProvinceName))
            {
                return null;
            }

            var province = FindProvince(cityResolveCriteria.ProvinceName);
            if (province == null)
            {
                return null;
            }

            var cityName = cityResolveCriteria.CityName.Trim();
            if (cityName.Length > 100)
            {
                cityName = cityName.Substring(0, 100);
            }

            var normalizedCityName = Normalize(cityName);
            var city = this.citiesRepository.GetAll().
                                             Where(x => x.ProvinceId == province.Id).
                                             ToList().
                                             FirstOrDefault(x => Normalize(x.Name) == normalizedCityName);

            if (city == null)
            {
                city = new City
                {
                    ProvinceId = province.Id,
                    Name = cityName,
                    PostalCode = String.IsNullOrWhiteSpace(cityResolveCriteria.PostalCode) ? "S/D" : cityResolveCriteria.PostalCode.Trim()
                };
                this.citiesRepository.Add(city);
                this.unitOfWork.Commit();
            }

            return Mapper.Map<City, CityDto>(city);
        }

        private Province FindProvince(String provinceName)
        {
            var normalized = Normalize(provinceName).Replace("provincia de ", "");
            var provinces = this.provincesRepository.GetAll().ToList();

            //Coincidencia exacta y, si no, por prefijo (ej: "Tierra del Fuego" dentro del nombre completo de la provincia)
            return provinces.FirstOrDefault(x => Normalize(x.Name) == normalized) ??
                   provinces.FirstOrDefault(x => Normalize(x.Name).StartsWith(normalized));
        }

        //Minusculas, sin tildes y sin espacios repetidos, para comparar nombres escritos de distinta forma
        private static String Normalize(String text)
        {
            var decomposed = (text ?? String.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var withoutAccents = new String(decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
            return String.Join(" ", withoutAccents.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
