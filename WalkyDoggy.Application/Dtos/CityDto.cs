using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities.Dtos;

namespace WalkyDoggy.Application.Dtos
{
    public class CityDto : IDto
    {
        public Int64 Id { get; set; }

        public String Name { get; set; }

        public String PostalCode { get; set; }
    }
}
