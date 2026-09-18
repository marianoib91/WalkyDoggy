using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Criterias;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Services.Contracts
{
    public interface ICityAppService : IEntityBaseAppService<City, CityDto>
    {
        List<CityDto> GetAll();

        List<CityDto> GetAllByProvinceId(Int64 provinceId);

        //Busca la ciudad por nombre dentro de su provincia y, si todavia no existe, la crea.
        //Devuelve null si la provincia no se reconoce.
        CityDto Resolve(CityResolveCriteria cityResolveCriteria);
    }
}
