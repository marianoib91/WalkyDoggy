using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Entities.Entities;

namespace WalkyDoggy.Services.Contracts
{
    public interface ITimeAppService
    {
        List<TimeDto> GetAvailableWalkTimes();

        List<TimeDto> GetAllWalkTimes();
    }
}
