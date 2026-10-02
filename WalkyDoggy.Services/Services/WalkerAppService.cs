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
        //Cantidad maxima de mascotas que un paseador puede llevar a la vez
        public const Int32 MaxPetsPerWalk = 5;

        #region Variables
        private readonly IEntityBaseRepository<Walker> walkersRepository;
        private readonly IEntityBaseRepository<Customer> customersRepository;
        private readonly IEntityBaseRepository<WorkDay> workDaysRepository;
        private readonly IEntityBaseRepository<Walk> walksRepository;
        private readonly IEntityBaseRepository<Ranking> rankingsRepository;
        private readonly IEntityBaseRepository<Price> pricesRepository;
        private readonly IEntityBaseRepository<UserRole> userRolesRepository;
        private readonly IEncryptionService encryptionService;
        private readonly IMembershipService membershipService;
        #endregion

        public WalkerAppService(IEntityBaseRepository<Error> errorsRepository,
                                IUnitOfWork unitOfWork,
                                IEntityBaseRepository<Walker> walkersRepository,
                                IEntityBaseRepository<Customer> customersRepository,
                                IEntityBaseRepository<WorkDay> workDaysRepository,
                                IEntityBaseRepository<Walk> walksRepository,
                                IEntityBaseRepository<Ranking> rankingsRepository,
                                IEntityBaseRepository<Price> pricesRepository,
                                IEntityBaseRepository<UserRole> userRolesRepository,
                                IEncryptionService encryptionService,
                                IMembershipService membershipService) :
            base(errorsRepository, unitOfWork, walkersRepository)
        {
            this.walkersRepository = walkersRepository;
            this.customersRepository = customersRepository;
            this.workDaysRepository = workDaysRepository;
            this.walksRepository = walksRepository;
            this.rankingsRepository = rankingsRepository;
            this.pricesRepository = pricesRepository;
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

            //Se registra el paseador, con la tarifa por hora que el mismo eligio
            var walker = Mapper.Map<WalkerDto, Walker>(walkerDto);
            walker.UserId = createdUser.Id;
            ApplyRate(walker, walkerDto.Amount);
            this.walkersRepository.Add(walker);

            //Se guardan los registros en la base de datos y se devuelve el Walker creado
            this.unitOfWork.Commit();

            //Se devuelve el roleId y el userId para usarlo en la presentacion de las vistas de acuerdo al rol del usuario
            walkerDto.RoleId = userRole.RoleId;
            walkerDto.UserId = walker.UserId;

            return walkerDto;
        }

        //Tarifa por hora que puede fijar un paseador (en pesos)
        public const Double MinRate = 1;
        public const Double MaxRate = 1000000;

        //La tarifa la fija cada paseador. Se reutiliza la tabla de precios: si ya existe un precio con ese monto se usa,
        //y si no se crea. Los paseos ya reservados conservan el precio con el que se pidieron.
        private void ApplyRate(Walker walker, Double amount)
        {
            var rate = Math.Round(amount, 2);

            var price = this.pricesRepository.GetAll().Where(x => x.Amount == rate).FirstOrDefault();
            if (price == null)
            {
                price = new Price { Amount = rate };
                this.pricesRepository.Add(price);
                walker.Price = price;
            }
            else
            {
                walker.PriceId = price.Id;
                walker.Price = price;
            }
        }

        //Completa el promedio y la cantidad de valoraciones de los paseadores
        private void AttachRatings(List<WalkerDto> walkersDto)
        {
            if (walkersDto.Count == 0)
            {
                return;
            }

            var stats = this.rankingsRepository.GetAll().
                                                GroupBy(x => x.WalkerId).
                                                Select(g => new { WalkerId = g.Key, Count = g.Count(), Average = g.Average(x => x.Score) }).
                                                ToList();

            foreach (var walkerDto in walkersDto)
            {
                var stat = stats.FirstOrDefault(x => x.WalkerId == walkerDto.Id);
                walkerDto.RatingCount = stat != null ? stat.Count : 0;
                walkerDto.AverageRating = stat != null ? Math.Round(stat.Average, 1) : (Double?)null;
            }
        }

        public WalkerDto GetByUserId(Int64 userId)
        {
            var walker = this.walkersRepository.AllIncluding(x => x.City, x => x.City.Province, x => x.Price).Where(x => x.UserId == userId).FirstOrDefault();
            var walkerDto = Mapper.Map<Walker, WalkerDto>(walker);
            if (walkerDto != null)
            {
                AttachRatings(new List<WalkerDto> { walkerDto });
            }
            return walkerDto;
        }

        public void Update(WalkerDto walkerDto)
        {
            //Se actualiza el paseador existente (y no una copia armada desde el DTO) para no pisar
            //con NULL las columnas que el DTO no trae, como los datos de la cuenta de Mercado Pago
            var walker = this.walkersRepository.GetSingle(walkerDto.Id);
            if (walker == null)
            {
                return;
            }

            var priceIdBefore = walker.PriceId;
            Mapper.Map(walkerDto, walker);

            //La tarifa por hora se ajusta con el monto que informa el paseador (si no informa ninguno, queda la que tenia)
            if (walkerDto.Amount > 0)
            {
                ApplyRate(walker, walkerDto.Amount);
            }
            else
            {
                walker.PriceId = priceIdBefore;
            }

            this.unitOfWork.Commit();
        }

        public List<WalkerDto> GetAll()
        {
            var walkers = this.walkersRepository.AllIncluding(x => x.City, x => x.Price, x => x.City.Province).ToList();
            var walkersDto = Mapper.Map<List<Walker>, List<WalkerDto>>(walkers);
            AttachRatings(walkersDto);
            return walkersDto;
        }

        public List<WalkerDto> GetAllOrderedByDistance(Int64 customerId)
        {
            var walkersDto = GetAll();

            var customer = this.customersRepository.GetSingle(customerId);
            double customerLatitude, customerLongitude;
            if (customer == null || !TryParseCoordinates(customer.Latitude, customer.Longitude, out customerLatitude, out customerLongitude))
            {
                return walkersDto;
            }

            foreach (var walkerDto in walkersDto)
            {
                double walkerLatitude, walkerLongitude;
                if (TryParseCoordinates(walkerDto.Latitude, walkerDto.Longitude, out walkerLatitude, out walkerLongitude))
                {
                    walkerDto.DistanceKm = Math.Round(DistanceInKilometers(customerLatitude, customerLongitude, walkerLatitude, walkerLongitude), 1);
                }
            }

            return walkersDto.OrderBy(x => x.DistanceKm.HasValue ? 0 : 1).
                              ThenBy(x => x.DistanceKm).
                              ToList();
        }

        private static Boolean TryParseCoordinates(String latitude, String longitude, out Double parsedLatitude, out Double parsedLongitude)
        {
            parsedLatitude = 0;
            parsedLongitude = 0;

            return Double.TryParse(latitude, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsedLatitude) &&
                   Double.TryParse(longitude, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsedLongitude);
        }

        //Distancia en linea recta entre dos puntos (formula de Haversine)
        private static Double DistanceInKilometers(Double latitude1, Double longitude1, Double latitude2, Double longitude2)
        {
            const Double earthRadiusKm = 6371;
            Func<Double, Double> toRadians = degrees => degrees * Math.PI / 180;

            var deltaLatitude = toRadians(latitude2 - latitude1);
            var deltaLongitude = toRadians(longitude2 - longitude1);
            var a = Math.Sin(deltaLatitude / 2) * Math.Sin(deltaLatitude / 2) +
                    Math.Cos(toRadians(latitude1)) * Math.Cos(toRadians(latitude2)) *
                    Math.Sin(deltaLongitude / 2) * Math.Sin(deltaLongitude / 2);

            return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        public WalkerDto GetDetail(Int64 id)
        {
            var walker = this.walkersRepository.AllIncluding(x => x.City, x => x.Price, x => x.City.Province).
                                                Where(x => x.Id == id).
                                                FirstOrDefault();
            if (walker == null)
            {
                return null;
            }

            var walkerDto = Mapper.Map<Walker, WalkerDto>(walker);
            AttachRatings(new List<WalkerDto> { walkerDto });
            return walkerDto;
        }

        //Horarios en los que el paseador puede recibir un paseo en la fecha indicada.
        //Un paseo dura una hora, por eso el horario de fin de la jornada no se ofrece como inicio.
        public List<String> GetAvailableTimes(Int64 walkerId, DateTime date)
        {
            var times = new List<String>();
            var day = date.Date;
            var now = DateTime.Now;

            if (day < now.Date)
            {
                return times;
            }

            var dayOfWeek = new DayOfWeekService().GetDayOfWeek(day.DayOfWeek.ToString());
            var workDays = this.workDaysRepository.GetAll().
                                                   Where(x => x.WalkerId == walkerId && x.DayOfWeek == dayOfWeek).
                                                   ToList();
            if (workDays.Count == 0)
            {
                return times;
            }

            var walks = this.walksRepository.GetAll().
                                             Where(x => x.WalkerId == walkerId && x.Date == day && x.Status != WalkStatus.Cancelled).
                                             ToList();

            foreach (var workDay in workDays)
            {
                var from = Convert.ToInt32(workDay.TimeFrom.Split(':')[0]);
                var until = Convert.ToInt32(workDay.TimeUntil.Split(':')[0]);

                for (var hour = from; hour < until; hour++)
                {
                    //Si es hoy, solo se ofrecen horarios posteriores a la hora actual
                    if (day == now.Date && hour <= now.Hour)
                    {
                        continue;
                    }

                    var time = hour.ToString("00") + ":00";
                    var petsInWalk = walks.Count(x => x.TimeFrom == time);
                    if (petsInWalk >= MaxPetsPerWalk)
                    {
                        continue;
                    }

                    if (!times.Contains(time))
                    {
                        times.Add(time);
                    }
                }
            }

            times.Sort();
            return times;
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
