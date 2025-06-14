using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Entities.Entities;

namespace WalkyDoggy.Services.Contracts
{
    public interface IWorkDayAppService : IEntityBaseAppService<WorkDay, WorkDayDto>
    {
        List<WorkDayDto> GetAll();

        List<WorkDayDto> GetAllByWalkerId(Int64 walkerId);

        void Delete(Int64 id);

        WorkDay Save(WorkDayDto workDayDto);
    }
}
