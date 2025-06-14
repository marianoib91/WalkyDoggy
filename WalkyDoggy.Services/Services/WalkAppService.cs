using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Criterias;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Services.Utilities;

namespace WalkyDoggy.Services.Services
{
    public class WalkAppService : EntityBaseAppService<Walk, WalkDto>, IWalkAppService
    {
        #region Variables
        private readonly IEntityBaseRepository<Walk> walksRepository;
        #endregion

        public WalkAppService(IEntityBaseRepository<Error> errorsRepository,
                                IUnitOfWork unitOfWork,
                                IEntityBaseRepository<Walk> walksRepository) :
            base(errorsRepository, unitOfWork, walksRepository)
        {
            this.walksRepository = walksRepository;
        }

        public List<WalkDto> GetAll()
        {
            var walks = this.walksRepository.GetAll().ToList();
            var walksDto = Mapper.Map<List<Walk>, List<WalkDto>>(walks);
            return walksDto;
        }

        public List<WalkDto> GetAllByWalkerId(Int64 walkerId)
        {
            var walks = this.walksRepository.GetAll().Where(x => x.WalkerId == walkerId).ToList();
            var walksDto = Mapper.Map<List<Walk>, List<WalkDto>>(walks);
            return walksDto;
        }

        public List<WalkDto> GetAllForCurrentDay(Int64 walkerId)
        {
            var currentdateAndTime = DateTime.Now;
            var currentHour = currentdateAndTime.Hour;
            var currentMinute = currentdateAndTime.Minute;
            var currentDate = currentdateAndTime.Date;

            var walks = this.walksRepository.AllIncluding(x => x.Pet, x => x.Pet.Customer,
                                                          x => x.Pet.Customer.City.Province).
                                             Where(x => x.WalkerId == walkerId &&
                                                        x.Date == currentDate && x.Confirmed == false).
                                             ToList();

            var availableWalks = new List<Walk>();
            foreach (var walk in walks)
            {
                var timeFrom = walk.TimeFrom;
                var hour = Convert.ToInt32(timeFrom.Split(':')[0]);
                if (currentHour >= hour)
                {
                    //Se setea como vencido para diferenciarlos en la vista web
                    walk.IsExpired = true;
                    availableWalks.Add(walk);

                }
                else
                {
                    availableWalks.Add(walk);
                }
            }

            var walksDto = Mapper.Map<List<Walk>, List<WalkDto>>(availableWalks);
            return walksDto;
        }

        public WalkDto ValidatePetsInWalks(AvailableWalkersCriteria availableWalkersCriteria)
        {
            var dayOfWeekService = new DayOfWeekService();
            var date = DateTime.Parse(availableWalkersCriteria.Date).Date;
            var dayOfWeek = dayOfWeekService.GetDayOfWeek(date.DayOfWeek.ToString());
            var timeFrom = Convert.ToInt64(availableWalkersCriteria.TimeFrom.Split(':')[0]);

            var walks = this.walksRepository.AllIncluding(x=>x.Pet).Where(x => x.Date == date &&
                                                               x.TimeFrom == availableWalkersCriteria.TimeFrom);
            var existingWalkWithSelectedPetDto = new WalkDto();
            if (walks.Count() > 0)
            {
                foreach (var petDto in availableWalkersCriteria.SelectedPets)
                {
                    var existingWalkWithSelectedPet = walks.Where(x => x.PetId == petDto.Id).FirstOrDefault();
                    if (existingWalkWithSelectedPet != null)
                    {
                        existingWalkWithSelectedPetDto = Mapper.Map<Walk, WalkDto>(existingWalkWithSelectedPet);
                        return existingWalkWithSelectedPetDto;
                    }
                }
            }
            else
            {
                return existingWalkWithSelectedPetDto;
            }
            return existingWalkWithSelectedPetDto;
        }
    }
}
