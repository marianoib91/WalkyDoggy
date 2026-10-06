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

            //La oferta de hospedaje se guarda por su propia pantalla (api/stays/saveOffer): actualizar el perfil no la toca
            Mapper.CreateMap<WalkerDto, Walker>().
                ForMember(p => p.BoardingEnabled, m => m.Ignore()).
                ForMember(p => p.BoardingPricePerNight, m => m.Ignore()).
                ForMember(p => p.BoardingMaxDogs, m => m.Ignore()).
                ForMember(p => p.BoardingDescription, m => m.Ignore());

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