using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Application.Criterias;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Entities.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Services.Dtos;
using WalkyDoggy.Services.Utilities;

namespace WalkyDoggy.Services.Services
{
    public class WalkerAppService : EntityBaseAppService<Walker, WalkerDto>, IWalkerAppService
    {
        #region Variables
        private readonly IEntityBaseRepository<Walker> walkersRepository;
        private readonly IEntityBaseRepository<WorkDay> workDaysRepository;
        private readonly IEntityBaseRepository<Walk> walksRepository;
        private readonly IEntityBaseRepository<UserRole> userRolesRepository;
        private readonly IEncryptionService encryptionService;
        private readonly IMembershipService membershipService;
        #endregion

        public WalkerAppService(IEntityBaseRepository<Error> errorsRepository,
                                IUnitOfWork unitOfWork,
                                IEntityBaseRepository<Walker> walkersRepository,
                                IEntityBaseRepository<WorkDay> workDaysRepository,
                                IEntityBaseRepository<Walk> walksRepository,
                                IEntityBaseRepository<UserRole> userRolesRepository,
                                IEncryptionService encryptionService,
                                IMembershipService membershipService) :
            base(errorsRepository, unitOfWork, walkersRepository)
        {
            this.walkersRepository = walkersRepository;
            this.workDaysRepository = workDaysRepository;
            this.walksRepository = walksRepository;
            this.userRolesRepository = userRolesRepository;
            this.encryptionService = encryptionService;
            this.membershipService = membershipService;
        }

        public WalkerDto Register(WalkerDto walkerDto)
        {
            //Se registra el usuario correspondiente al paseador
            var userDto = new UserDto();
            userDto.Email = walkerDto.Email;
            userDto.CreatedDate = DateTime.Now;
            userDto.IsLocked = false;
            userDto.Password = walkerDto.Password;
            var createdUser = this.membershipService.CreateUser(userDto);

            //Se registra el rol del usuario en la tabla UserRoles
            var userRole = new UserRole();
            userRole.UserId = createdUser.Id;
            userRole.RoleId = Roles.Walker;
            this.userRolesRepository.Add(userRole);

            //Se registra el paseador
            var walker = Mapper.Map<WalkerDto, Walker>(walkerDto);
            walker.UserId = createdUser.Id;
            this.walkersRepository.Add(walker);

            //Se guardan los registros en la base de datos y se devuelve el Walker creado
            this.unitOfWork.Commit();

            //Se devuelve el roleId y el userId para usarlo en la presentacion de las vistas de acuerdo al rol del usuario
            walkerDto.RoleId = userRole.RoleId;
            walkerDto.UserId = walker.UserId;

            return walkerDto;
        }

        public WalkerDto GetByUserId(Int64 userId)
        {
            var walker = this.walkersRepository.AllIncluding(x => x.City, x => x.Price).Where(x => x.UserId == userId).FirstOrDefault();
            var walkerDto = Mapper.Map<Walker, WalkerDto>(walker);
            return walkerDto;
        }

        public void Update(WalkerDto walkerDto)
        {
            var walker = Mapper.Map<WalkerDto, Walker>(walkerDto);
            this.walkersRepository.Edit(walker);
            this.unitOfWork.Commit();
        }

        public List<WalkerDto> GetAll()
        {
            var walkers = this.walkersRepository.AllIncluding(x => x.City, x => x.Price, x => x.City.Province).ToList();
            var walkersDto = Mapper.Map<List<Walker>, List<WalkerDto>>(walkers);
            return walkersDto;
        }

        public List<WalkerDto> GetAvailableWalkers(AvailableWalkersCriteria availableWalkersCriteria)
        {
            var dayOfWeekService = new DayOfWeekService();
            var date = DateTime.Parse(availableWalkersCriteria.Date).Date;
            var dayOfWeek = dayOfWeekService.GetDayOfWeek(date.DayOfWeek.ToString());
            var timeFrom = Convert.ToInt64(availableWalkersCriteria.TimeFrom.Split(':')[0]);

            //Se obtienen las jornadas laborales del dia de la semana seleccionado
            var workDays = this.workDaysRepository.AllIncluding(x => x.Walker.City.Province,
                                                                x => x.Walker.Price).
                                                               Where(x => x.DayOfWeek == dayOfWeek).
                                                               ToList();

            //Se obtienen los paseos registrados con ese dia y hora
            var walks = this.walksRepository.GetAll().Where(x => x.Date == date &&
                                                               x.TimeFrom == availableWalkersCriteria.TimeFrom).
                                                      ToList();

          
            //Listado de paseadores disponibles
            var availableWalkers = new List<Walker>();

            foreach (var workDay in workDays)
            {
                //Se valida que el horario seleccionado este dentro de las jornadas laborales seleccionadas anteriormente
                if (Convert.ToInt64(workDay.TimeFrom.Split(':')[0]) <= timeFrom &&
                    Convert.ToInt64(workDay.TimeUntil.Split(':')[0]) >= timeFrom)
                {
                    //Si hay paseos ese dia y a esa hora se hacen las validaciones correspondientes
                    if (walks.Count > 0)
                    {
                        var walkerWalks = walks.Where(x => x.WalkerId == workDay.WalkerId);

                        //Puede pasear hasta 5 mascotas a la vez
                        if (walkerWalks.Count() > 5)
                        {
                            //si tiene mas de 5 para ese dia y hora no se muestra el paseador
                        }
                        else
                        {
                            availableWalkers.Add(workDay.Walker);
                        }
                    }
                    //Se agrega directamente el paseador disponible en ese dia y hora porque no hay paseos con ese dia y hora
                    else
                    {
                        availableWalkers.Add(workDay.Walker);
                    }
                }
            }
            var walkersDto = Mapper.Map<List<Walker>, List<WalkerDto>>(availableWalkers);
            return walkersDto;
        }
    }
}
