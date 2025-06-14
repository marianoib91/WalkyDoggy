using AutoMapper;
using WalkyDoggy.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WalkyDoggy.Services.Dtos;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Entities.Entities;

namespace WalkyDoggy.Web.Mappings
{
    public class DtoToDomainMappingProfile : Profile
    {
        public override string ProfileName
        {
            get { return "DtoToDomainMappings"; }
        }

        protected override void Configure()
        {
            Mapper.CreateMap<CustomerDto, Customer>();

            Mapper.CreateMap<UserDto, User>();

            Mapper.CreateMap<WalkerDto, Walker>();

            Mapper.CreateMap<PetDto, Pet>();

            Mapper.CreateMap<BreedDto, Breed>();

            Mapper.CreateMap<SizeDto, Size>();

            Mapper.CreateMap<ProvinceDto, Province>();

            Mapper.CreateMap<CityDto, City>();

            Mapper.CreateMap<WalkDto, Walk>();

            Mapper.CreateMap<PriceDto, Price>();

            Mapper.CreateMap<WorkDayDto, WorkDay>();
        }
    }
}