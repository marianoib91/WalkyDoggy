using AutoMapper;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly IEntityBaseRepository<Walker> walkersRepository;
        private readonly IEntityBaseRepository<Pet> petsRepository;
        private readonly IWalkerAppService walkerAppService;
        #endregion

        public WalkAppService(IEntityBaseRepository<Error> errorsRepository,
                                IUnitOfWork unitOfWork,
                                IEntityBaseRepository<Walk> walksRepository,
                                IEntityBaseRepository<Walker> walkersRepository,
                                IEntityBaseRepository<Pet> petsRepository,
                                IWalkerAppService walkerAppService) :
            base(errorsRepository, unitOfWork, walksRepository)
        {
            this.walksRepository = walksRepository;
            this.walkersRepository = walkersRepository;
            this.petsRepository = petsRepository;
            this.walkerAppService = walkerAppService;
        }

        public List<WalkDto> Register(WalkRequestCriteria walkRequestCriteria, out String error)
        {
            error = null;

            if (walkRequestCriteria == null || walkRequestCriteria.PetIds == null || walkRequestCriteria.PetIds.Count == 0)
            {
                error = "Debe seleccionar al menos una mascota para el paseo.";
                return null;
            }

            DateTime date;
            if (!DateTime.TryParseExact(walkRequestCriteria.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out date))
            {
                error = "La fecha del paseo no es válida.";
                return null;
            }

            var walker = this.walkersRepository.GetSingle(walkRequestCriteria.WalkerId);
            if (walker == null)
            {
                error = "El paseador seleccionado no existe.";
                return null;
            }

            //Se valida contra la agenda actual del paseador, por si el horario se ocupo mientras se reservaba
            var availableTimes = this.walkerAppService.GetAvailableTimes(walker.Id, date);
            if (!availableTimes.Contains(walkRequestCriteria.TimeFrom))
            {
                error = "El paseador ya no está disponible en el día y horario seleccionados.";
                return null;
            }

            var petIds = walkRequestCriteria.PetIds.Distinct().ToList();
            var pets = this.petsRepository.GetAll().Where(x => petIds.Contains(x.Id)).ToList();
            if (pets.Count != petIds.Count)
            {
                error = "Alguna de las mascotas seleccionadas no existe.";
                return null;
            }

            var timeFrom = walkRequestCriteria.TimeFrom;
            var walksAtSameTime = this.walksRepository.GetAll().
                                                       Where(x => x.Date == date && x.TimeFrom == timeFrom).
                                                       ToList();

            var busyPet = pets.FirstOrDefault(pet => walksAtSameTime.Any(walk => walk.PetId == pet.Id));
            if (busyPet != null)
            {
                error = busyPet.Name + " ya tiene reservado un paseo a la misma hora y día seleccionados.";
                return null;
            }

            var petsAlreadyBooked = walksAtSameTime.Count(x => x.WalkerId == walker.Id);
            if (petsAlreadyBooked + pets.Count > WalkerAppService.MaxPetsPerWalk)
            {
                error = "El paseador solo puede llevar hasta " + WalkerAppService.MaxPetsPerWalk +
                        " mascotas a la vez en ese horario.";
                return null;
            }

            var walks = new List<Walk>();
            foreach (var pet in pets)
            {
                var walk = new Walk
                {
                    WalkerId = walker.Id,
                    PetId = pet.Id,
                    PriceId = walker.PriceId,
                    Date = date,
                    TimeFrom = timeFrom,
                    Details = walkRequestCriteria.Details,
                    Confirmed = false
                };
                this.walksRepository.Add(walk);
                walks.Add(walk);
            }

            this.unitOfWork.Commit();

            return walks.Select(walk => new WalkDto
            {
                Id = walk.Id,
                Date = walk.Date,
                TimeFrom = walk.TimeFrom,
                Details = walk.Details,
                PetName = pets.First(pet => pet.Id == walk.PetId).Name
            }).ToList();
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
