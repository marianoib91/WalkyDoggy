using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities.Dtos;

namespace WalkyDoggy.Application.Dtos
{
    public class WalkerDto : IDto
    {
        public Int64 Id { get; set; }

        public Int64 UserId { get; set; }

        public Int64 PriceId { get; set; }

        public Int64 CityId { get; set; }

        public Int64 RoleId { get; set; }

        public Int64 ProvinceId { get; set; }

        public String FirstName { get; set; }

        public String LastName { get; set; }

        public String Email { get; set; }

        public String Description { get; set; }

        public String Phone { get; set; }

        public String StreetName { get; set; }

        public Int64 StreetNumber { get; set; }

        public String ProfileImage { get; set; }

        public String Latitude { get; set; }

        public String Longitude { get; set; }

        public String Password { get; set; }

        public String ProvinceName { get; set; }

        public String CityName { get; set; }

        public Double Amount { get; set; }

        //Distancia en km al domicilio del cliente (solo cuando se consulta con un cliente que tiene coordenadas)
        public Double? DistanceKm { get; set; }
    }
}
