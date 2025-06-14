using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Services.Contracts
{
    public interface ICityAppService : IEntityBaseAppService<City, CityDto>
    {
        List<CityDto> GetAll();

        List<CityDto> GetAllByProvinceId(Int64 provinceId);
    }
}
