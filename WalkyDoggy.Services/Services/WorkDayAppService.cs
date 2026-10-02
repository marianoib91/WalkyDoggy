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

        private static readonly String[] Days = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };

        //Reemplaza todas las franjas horarias del paseador por las indicadas. Se puede trabajar cualquier dia a cualquier hora
        //(de 00:00 a 24:00). Las franjas que se pisan o quedan pegadas se unen en una sola.
        public Boolean SaveWeek(Int64 walkerId, List<WorkRangeDto> ranges, out String error)
        {
            error = null;

            var worked = new Dictionary<String, Boolean[]>();
            foreach (var day in Days)
            {
                worked[day] = new Boolean[24];
            }

            foreach (var range in ranges ?? new List<WorkRangeDto>())
            {
                var day = Days.FirstOrDefault(x => x == range.DayOfWeek);
                if (day == null)
                {
                    error = "El día \"" + range.DayOfWeek + "\" no es válido.";
                    return false;
                }

                Int32 from, until;
                if (!TryParseHour(range.TimeFrom, 0, 23, out from) || !TryParseHour(range.TimeUntil, 1, 24, out until) || until <= from)
                {
                    error = "El horario " + range.TimeFrom + " a " + range.TimeUntil + " del " + day + " no es válido: usá horas en punto y que termine después de empezar.";
                    return false;
                }

                for (var hour = from; hour < until; hour++)
                {
                    worked[day][hour] = true;
                }
            }

            foreach (var existing in this.workDaysRepository.GetAll().Where(x => x.WalkerId == walkerId).ToList())
            {
                this.workDaysRepository.Delete(existing);
            }

            foreach (var day in Days)
            {
                var hour = 0;
                while (hour < 24)
                {
                    if (!worked[day][hour])
                    {
                        hour++;
                        continue;
                    }

                    var start = hour;
                    while (hour < 24 && worked[day][hour])
                    {
                        hour++;
                    }

                    this.workDaysRepository.Add(new WorkDay
                    {
                        WalkerId = walkerId,
                        DayOfWeek = day,
                        TimeFrom = start.ToString("00") + ":00",
                        TimeUntil = hour.ToString("00") + ":00"
                    });
                }
            }

            this.unitOfWork.Commit();
            return true;
        }

        private static Boolean TryParseHour(String text, Int32 min, Int32 max, out Int32 hour)
        {
            hour = 0;
            var match = System.Text.RegularExpressions.Regex.Match(text ?? String.Empty, @"^(\d{1,2}):00$");
            return match.Success && Int32.TryParse(match.Groups[1].Value, out hour) && hour >= min && hour <= max;
        }

        public void Delete(Int64 id)
        {
            var workDay = this.entityRepository.GetSingle(id);
            this.entityRepository.Delete(workDay);
            this.unitOfWork.Commit();

        }
    }
}
