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
    public interface IWalkAppService : IEntityBaseAppService<Walk, WalkDto>
    {
        List<WalkDto> GetAll();

        List<WalkDto> GetAllByWalkerId(Int64 walkerId);

        List<WalkDto> GetAllForCurrentDay(Int64 walkerId);

        WalkDto ValidatePetsInWalks(AvailableWalkersCriteria availableWalkersCriteria);
    }
}
