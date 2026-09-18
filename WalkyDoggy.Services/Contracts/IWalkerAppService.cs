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
    public interface IWalkerAppService : IEntityBaseAppService<Walker, WalkerDto>
    {
        WalkerDto Register(WalkerDto walkerDto);

        WalkerDto GetByUserId(Int64 userId);

        void Update(WalkerDto walkerDto);

        List<WalkerDto> GetAvailableWalkers(AvailableWalkersCriteria availableWalkersCritera);

        List<WalkerDto> GetAll();

        //Todos los paseadores ordenados por cercania al domicilio del cliente (los que no tienen coordenadas van al final)
        List<WalkerDto> GetAllOrderedByDistance(Int64 customerId);

        WalkerDto GetDetail(Int64 id);

        List<String> GetAvailableTimes(Int64 walkerId, DateTime date);
    }
}
