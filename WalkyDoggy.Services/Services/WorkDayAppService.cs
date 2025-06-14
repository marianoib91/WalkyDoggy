using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
    public class WorkDayAppService : EntityBaseAppService<WorkDay, WorkDayDto>, IWorkDayAppService
    {
        #region Variables
        private readonly IEntityBaseRepository<WorkDay> workDaysRepository;
        #endregion

        public WorkDayAppService(IEntityBaseRepository<Error> errorsRepository,
                                  IUnitOfWork unitOfWork,
                                  IEntityBaseRepository<WorkDay> workDaysRepository) :
            base(errorsRepository, unitOfWork, workDaysRepository)
        {
            this.workDaysRepository = workDaysRepository;
        }

        public List<WorkDayDto> GetAll()
        {
            var workDays = this.entityRepository.GetAll().ToList();
            var workDaysDto = Mapper.Map<List<WorkDay>, List<WorkDayDto>>(workDays);
            return workDaysDto;
        }


        public List<WorkDayDto> GetAllByWalkerId(Int64 walkerId)
        {
            var workDays = this.entityRepository.GetAll().Where(x => x.WalkerId == walkerId).ToList();
            var workDaysDto = Mapper.Map<List<WorkDay>, List<WorkDayDto>>(workDays);
            return workDaysDto;
        }

        public WorkDay Save(WorkDayDto workDayDto)
        {
            if (workDayDto.Id == 0)
            {
                var existingWorkDay = this.entityRepository.GetAll().Where(x => x.WalkerId == workDayDto.WalkerId &&
                                                                              x.DayOfWeek == workDayDto.DayOfWeek &&
                                                                              x.TimeFrom == workDayDto.TimeFrom &&
                                                                              x.TimeUntil == workDayDto.TimeUntil).FirstOrDefault();
                if (existingWorkDay == null)
                {
                    var workDay = Mapper.Map<WorkDayDto, WorkDay>(workDayDto);
                    this.entityRepository.Add(workDay);
                    this.unitOfWork.Commit();
                    return workDay;
                }
                else
                {
                    return null;
                }
             
            }
            else
            {
                var workDay = Mapper.Map<WorkDayDto, WorkDay>(workDayDto);
                this.entityRepository.Edit(workDay);
                this.unitOfWork.Commit();
                return workDay;
            }

        }

        public void Delete(Int64 id)
        {
            var workDay = this.entityRepository.GetSingle(id);
            this.entityRepository.Delete(workDay);
            this.unitOfWork.Commit();

        }
    }
}
