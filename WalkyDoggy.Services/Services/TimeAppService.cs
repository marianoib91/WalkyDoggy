using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Entities.Entities;
using WalkyDoggy.Services.Contracts;

namespace WalkyDoggy.Services.Services
{
    public class TimeAppService : ITimeAppService
    {

        public TimeAppService()
        {
        }
        public List<TimeDto> GetAvailableWalkTimes()
        {
            var currentTime = DateTime.Now.TimeOfDay;
            var timesDto = new List<TimeDto>();
            return timesDto;
        }

        public List<TimeDto> GetAllWalkTimes()
        {
            var currentTime = DateTime.Now.TimeOfDay;
            var timesDto = new List<TimeDto>();
            return timesDto;
        }
    }
}
