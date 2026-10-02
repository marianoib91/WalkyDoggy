using AutoMapper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Constants;
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
        private readonly IEntityBaseRepository<Customer> customersRepository;
        private readonly IEntityBaseRepository<City> citiesRepository;
        private readonly IWalkerAppService walkerAppService;
        private readonly IPaymentAppService paymentAppService;
        #endregion

        public WalkAppService(IEntityBaseRepository<Error> errorsRepository,
                                IUnitOfWork unitOfWork,
                                IEntityBaseRepository<Walk> walksRepository,
                                IEntityBaseRepository<Walker> walkersRepository,
                                IEntityBaseRepository<Pet> petsRepository,
                                IEntityBaseRepository<Customer> customersRepository,
                                IEntityBaseRepository<City> citiesRepository,
                                IWalkerAppService walkerAppService,
                                IPaymentAppService paymentAppService) :
            base(errorsRepository, unitOfWork, walksRepository)
        {
            this.walksRepository = walksRepository;
            this.walkersRepository = walkersRepository;
            this.petsRepository = petsRepository;
            this.customersRepository = customersRepository;
            this.citiesRepository = citiesRepository;
            this.walkerAppService = walkerAppService;
            this.paymentAppService = paymentAppService;
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

            //Direccion de retiro: la elegida para este paseo o, si no se informa, el domicilio del cliente.
            //Siempre se guarda una copia, asi el paseo conserva su direccion aunque el cliente cambie su perfil.
            var pickup = ResolvePickup(walkRequestCriteria, pets, out error);
            if (pickup == null)
            {
                return null;
            }

            var paymentMethod = String.IsNullOrWhiteSpace(walkRequestCriteria.PaymentMethod) ? PaymentMethods.Cash : walkRequestCriteria.PaymentMethod;
            if (paymentMethod != PaymentMethods.Cash && paymentMethod != PaymentMethods.MercadoPago)
            {
                error = "El método de pago no es válido.";
                return null;
            }

            var timeFrom = walkRequestCriteria.TimeFrom;
            var walksAtSameTime = this.walksRepository.GetAll().
                                                       Where(x => x.Date == date && x.TimeFrom == timeFrom && x.Status != WalkStatus.Cancelled).
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

            //Los paseos de una misma reserva (uno por mascota) comparten el BookingCode y arrancan esperando la respuesta del paseador
            var bookingCode = Guid.NewGuid();
            var walks = new List<Walk>();
            foreach (var pet in pets)
            {
                var walk = new Walk
                {
                    Status = WalkStatus.Pending,
                    BookingCode = bookingCode,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = PaymentStatuses.Pending,
                    WalkerId = walker.Id,
                    PetId = pet.Id,
                    PriceId = walker.PriceId,
                    Date = date,
                    TimeFrom = timeFrom,
                    Details = walkRequestCriteria.Details,
                    Confirmed = false,
                    PickupStreetName = pickup.StreetName,
                    PickupStreetNumber = pickup.StreetNumber,
                    PickupCityId = pickup.CityId,
                    PickupLatitude = pickup.Latitude,
                    PickupLongitude = pickup.Longitude
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

        private class PickupAddress
        {
            public String StreetName { get; set; }
            public Int64? StreetNumber { get; set; }
            public Int64? CityId { get; set; }
            public String Latitude { get; set; }
            public String Longitude { get; set; }
        }

        private PickupAddress ResolvePickup(WalkRequestCriteria walkRequestCriteria, List<Pet> pets, out String error)
        {
            error = null;

            if (!String.IsNullOrWhiteSpace(walkRequestCriteria.PickupStreetName))
            {
                var streetName = walkRequestCriteria.PickupStreetName.Trim();
                if (streetName.Length > 50 ||
                    !walkRequestCriteria.PickupStreetNumber.HasValue || walkRequestCriteria.PickupStreetNumber.Value <= 0 ||
                    !walkRequestCriteria.PickupCityId.HasValue ||
                    this.citiesRepository.GetSingle(walkRequestCriteria.PickupCityId.Value) == null)
                {
                    error = "La dirección de retiro no es válida. Elegila de la lista de sugerencias.";
                    return null;
                }

                return new PickupAddress
                {
                    StreetName = streetName,
                    StreetNumber = walkRequestCriteria.PickupStreetNumber,
                    CityId = walkRequestCriteria.PickupCityId,
                    Latitude = walkRequestCriteria.PickupLatitude,
                    Longitude = walkRequestCriteria.PickupLongitude
                };
            }

            var customerIds = pets.Select(x => x.CustomerId).Distinct().ToList();
            var customer = customerIds.Count == 1 ? this.customersRepository.GetSingle(customerIds[0]) : null;
            if (customer == null)
            {
                error = "Las mascotas del paseo deben ser de un mismo cliente.";
                return null;
            }

            return new PickupAddress
            {
                StreetName = customer.StreetName,
                StreetNumber = customer.StreetNumber,
                CityId = customer.CityId,
                Latitude = customer.Latitude,
                Longitude = customer.Longitude
            };
        }

        /* ---------- Reservas: confirmar, cancelar y vencer ---------- */

        public List<BookingDto> GetBookingsForWalker(Int64 walkerId)
        {
            ExpirePendingWalks();

            var today = DateTime.Now.Date;
            var walks = IncludeBookingData().
                        Where(x => x.WalkerId == walkerId && x.Date >= today && x.Status != WalkStatus.Cancelled).
                        ToList();

            return BuildBookings(walks).
                   OrderBy(x => x.Date).
                   ThenBy(x => x.TimeFrom).
                   ToList();
        }

        public List<BookingDto> GetBookingsForCustomer(Int64 customerId)
        {
            ExpirePendingWalks();

            var walks = IncludeBookingData().
                        Where(x => x.Pet.CustomerId == customerId).
                        ToList();

            return BuildBookings(walks).
                   OrderByDescending(x => x.Date).
                   ThenByDescending(x => x.TimeFrom).
                   ToList();
        }

        public Boolean Confirm(BookingActionCriteria bookingActionCriteria, out String error)
        {
            error = null;
            ExpirePendingWalks();

            var walks = FindBooking(bookingActionCriteria.BookingKey);
            if (walks.Count == 0 || walks[0].WalkerId != bookingActionCriteria.ActorId)
            {
                error = "La reserva no existe.";
                return false;
            }

            if (walks.Any(x => x.Status != WalkStatus.Pending))
            {
                error = walks[0].Status == WalkStatus.Cancelled
                    ? "Esta reserva ya fue cancelada."
                    : "Esta reserva ya estaba confirmada.";
                return false;
            }

            walks.ForEach(x => SetStatus(x, WalkStatus.Confirmed, null));
            this.unitOfWork.Commit();
            return true;
        }

        public Boolean Cancel(BookingActionCriteria bookingActionCriteria, out String error)
        {
            error = null;
            ExpirePendingWalks();

            var actor = bookingActionCriteria.Actor;
            if (actor != WalkCancelledBy.Walker && actor != WalkCancelledBy.Customer)
            {
                error = "No se puede cancelar la reserva.";
                return false;
            }

            var walks = FindBooking(bookingActionCriteria.BookingKey);
            var isOwner = walks.Count > 0 &&
                          (actor == WalkCancelledBy.Walker
                              ? walks[0].WalkerId == bookingActionCriteria.ActorId
                              : walks[0].Pet.CustomerId == bookingActionCriteria.ActorId);
            if (!isOwner)
            {
                error = "La reserva no existe.";
                return false;
            }

            if (walks.All(x => x.Status == WalkStatus.Cancelled))
            {
                error = "Esta reserva ya estaba cancelada.";
                return false;
            }

            if (StartOf(walks[0]) <= DateTime.Now)
            {
                error = "El paseo ya comenzó y no se puede cancelar.";
                return false;
            }

            //Si el cliente ya habia pagado con Mercado Pago, el dinero retenido se le devuelve antes de cancelar
            if (!this.paymentAppService.Refund(walks, out error))
            {
                error = "No se pudo devolver el pago, por eso el paseo no se canceló. " + error;
                return false;
            }

            walks.Where(x => x.Status != WalkStatus.Cancelled).ToList().ForEach(x => SetStatus(x, WalkStatus.Cancelled, actor));
            this.unitOfWork.Commit();
            return true;
        }

        //Reglas que dependen de la hora: un pedido que el paseador no respondio antes del paseo se cancela solo,
        //lo mismo que un paseo confirmado que el cliente no pago a tiempo, y los pagos sin reclamo se liberan.
        private void ExpirePendingWalks()
        {
            var now = DateTime.Now;
            var openWalks = this.walksRepository.GetAll().
                                                 Where(x => x.Date <= now.Date &&
                                                            (x.Status == WalkStatus.Pending ||
                                                             (x.Status == WalkStatus.Confirmed &&
                                                              x.PaymentMethod == PaymentMethods.MercadoPago &&
                                                              x.PaymentStatus == PaymentStatuses.Pending))).
                                                 ToList();

            var expired = openWalks.Where(x => StartOf(x) <= now).ToList();
            if (expired.Count > 0)
            {
                expired.ForEach(x => SetStatus(x, WalkStatus.Cancelled,
                                               x.Status == WalkStatus.Pending ? WalkCancelledBy.System : WalkCancelledBy.NoPayment));
                this.unitOfWork.Commit();
            }

            this.paymentAppService.ReleaseDuePayments();
        }

        private static DateTime StartOf(Walk walk)
        {
            return BookingHelper.StartOf(walk);
        }

        private static void SetStatus(Walk walk, String status, String cancelledBy)
        {
            walk.Status = status;
            walk.Confirmed = status == WalkStatus.Confirmed;
            walk.CancelledBy = cancelledBy;
            walk.StatusChangedAt = DateTime.Now;
        }

        private static String BookingKeyOf(Walk walk)
        {
            return BookingHelper.KeyOf(walk);
        }

        private List<Walk> FindBooking(String bookingKey)
        {
            return BookingHelper.Find(IncludeBookingData(), bookingKey);
        }

        private IQueryable<Walk> IncludeBookingData()
        {
            return this.walksRepository.AllIncluding(x => x.Pet, x => x.Pet.Customer,
                                                     x => x.Pet.Customer.City, x => x.Pet.Customer.City.Province,
                                                     x => x.PickupCity, x => x.PickupCity.Province,
                                                     x => x.Walker, x => x.Price);
        }

        private List<BookingDto> BuildBookings(List<Walk> walks)
        {
            return walks.GroupBy(BookingKeyOf).Select(group =>
            {
                var first = group.First();
                var customer = first.Pet.Customer;
                var usesPickup = first.PickupStreetName != null;
                var pricePerPet = first.Price != null ? first.Price.Amount : 0;

                string location;
                if (usesPickup)
                {
                    location = first.PickupStreetName + " " + first.PickupStreetNumber + " - " +
                               (first.PickupCity != null ? first.PickupCity.Name + " - " + (first.PickupCity.Province != null ? first.PickupCity.Province.Name : "") : "");
                }
                else
                {
                    location = customer.StreetName + " " + customer.StreetNumber + " - " +
                               (customer.City != null ? customer.City.Name + " - " + (customer.City.Province != null ? customer.City.Province.Name : "") : "");
                }

                return new BookingDto
                {
                    BookingKey = group.Key,
                    WalkerId = first.WalkerId,
                    WalkerName = first.Walker != null ? first.Walker.FirstName + " " + first.Walker.LastName : null,
                    WalkerProfileImage = first.Walker != null ? first.Walker.ProfileImage : null,
                    CustomerId = customer.Id,
                    CustomerFullName = customer.FirstName + " " + customer.LastName,
                    Date = first.Date,
                    TimeFrom = first.TimeFrom,
                    Status = first.Status,
                    CancelledBy = first.CancelledBy,
                    PaymentMethod = first.PaymentMethod,
                    PaymentStatus = first.PaymentStatus,
                    Details = first.Details,
                    Location = location,
                    Latitude = usesPickup ? first.PickupLatitude : customer.Latitude,
                    Longitude = usesPickup ? first.PickupLongitude : customer.Longitude,
                    PricePerPet = pricePerPet,
                    Total = pricePerPet * group.Count(),
                    Pets = group.Select(x => new BookingPetDto { Id = x.Pet.Id, Name = x.Pet.Name, ProfileImage = x.Pet.ProfileImage }).ToList()
                };
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
                                                          x => x.Pet.Customer.City.Province,
                                                          x => x.PickupCity, x => x.PickupCity.Province).
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

            var walks = this.walksRepository.AllIncluding(x=>x.Pet).Where(x => x.Date == date && x.Status != WalkStatus.Cancelled &&
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
