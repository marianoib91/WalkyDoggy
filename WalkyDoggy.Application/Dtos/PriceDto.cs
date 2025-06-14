using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities.Dtos;

namespace WalkyDoggy.Application.Dtos
{
    public class PriceDto : IDto
    {
        public Int64 Id { get; set; }

        public Double Amount { get; set; }
    }
}
