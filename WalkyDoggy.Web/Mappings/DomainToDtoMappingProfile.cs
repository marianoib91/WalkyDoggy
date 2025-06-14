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
    public class DomainToDtoMappingProfile : Profile
    {
        public override string ProfileName
        {
            get { return "DomainToDtoModelMappings"; }
        }

        protected override void Configure()
        {
            Mapper.CreateMap<Customer, CustomerDto>().
                ForMember(p => p.ProvinceId, m => m.MapFrom(s => s.City.ProvinceId)).
                ForMember(p => p.RoleId, m => m.MapFrom(s => s.User.UserRoles.Select(x => x.RoleId).FirstOrDefault()));

            Mapper.CreateMap<User, UserDto>();

            Mapper.CreateMap<Walker, WalkerDto>().
                ForMember(p => p.ProvinceId, m => m.MapFrom(s => s.City.ProvinceId)).
                ForMember(p => p.RoleId, m => m.MapFrom(s => s.User.UserRoles.Select(x => x.RoleId).FirstOrDefault())).
                ForMember(p => p.CityName, m => m.MapFrom(s => s.City.Name)).
                ForMember(p => p.ProvinceName, m => m.MapFrom(s => s.City.Province.Name)).
                ForMember(p => p.Amount, m => m.MapFrom(s => s.Price.Amount));

            Mapper.CreateMap<Pet, PetDto>().
                ForMember(p => p.BreedName, m => m.MapFrom(s => s.Breed.Name)).
                ForMember(p => p.SizeName, m => m.MapFrom(s => s.Size.Name));

            Mapper.CreateMap<Breed, BreedDto>();

            Mapper.CreateMap<Size, SizeDto>();

            Mapper.CreateMap<Province, ProvinceDto>();

            Mapper.CreateMap<City, CityDto>();

            Mapper.CreateMap<Walk, WalkDto>().
                 ForMember(p => p.PetName, m => m.MapFrom(s => s.Pet.Name)).
                 ForMember(p => p.CustomerFullName, m => m.MapFrom(s => s.Pet.Customer.FirstName + " " + s.Pet.Customer.LastName)).
                 ForMember(p => p.Location, m => m.MapFrom(s => s.Pet.Customer.StreetName + " " + s.Pet.Customer.StreetNumber + " - " + s.Pet.Customer.City.Name + " - " + s.Pet.Customer.City.Province.Name));

            Mapper.CreateMap<Price, PriceDto>();

            Mapper.CreateMap<WorkDay, WorkDayDto>();
        }
    }
}