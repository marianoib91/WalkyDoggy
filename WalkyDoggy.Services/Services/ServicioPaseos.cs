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
    public class ServicioPaseos : ServicioEntidadBase<Walk, WalkDto>, IServicioPaseos
    {
        #region Variables
        private readonly IRepositorioEntidadBase<Walk> repositorioPaseos;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;
        private readonly IRepositorioEntidadBase<Pet> repositorioMascotas;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;
        private readonly IRepositorioEntidadBase<City> repositorioCiudades;
        private readonly IServicioPaseadores servicioPaseadores;
        private readonly IProveedorTokensVendedor proveedorTokens;
        private readonly IRepositorioEntidadBase<Ranking> repositorioValoraciones;
        private readonly IServicioNotificaciones servicioNotificaciones;
        #endregion

        public ServicioPaseos(IRepositorioEntidadBase<Error> repositorioErrores,
                                IUnidadDeTrabajo unidadDeTrabajo,
                                IRepositorioEntidadBase<Walk> repositorioPaseos,
                                IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                IRepositorioEntidadBase<Pet> repositorioMascotas,
                                IRepositorioEntidadBase<Customer> repositorioClientes,
                                IRepositorioEntidadBase<City> repositorioCiudades,
                                IServicioPaseadores servicioPaseadores,
                                IProveedorTokensVendedor proveedorTokens,
                                IRepositorioEntidadBase<Ranking> repositorioValoraciones,
                                IServicioNotificaciones servicioNotificaciones) :
            base(repositorioErrores, unidadDeTrabajo, repositorioPaseos)
        {
            this.repositorioPaseos = repositorioPaseos;
            this.repositorioPaseadores = repositorioPaseadores;
            this.repositorioMascotas = repositorioMascotas;
            this.repositorioClientes = repositorioClientes;
            this.repositorioCiudades = repositorioCiudades;
            this.servicioPaseadores = servicioPaseadores;
            this.proveedorTokens = proveedorTokens;
            this.repositorioValoraciones = repositorioValoraciones;
            this.servicioNotificaciones = servicioNotificaciones;
        }

        public List<WalkDto> Registrar(WalkRequestCriteria criterioSolicitudPaseo, out String error)
        {
            error = null;

            if (criterioSolicitudPaseo == null || criterioSolicitudPaseo.PetIds == null || criterioSolicitudPaseo.PetIds.Count == 0)
            {
                error = "Debe seleccionar al menos una mascota para el paseo.";
                return null;
            }

            DateTime fecha;
            if (!DateTime.TryParseExact(criterioSolicitudPaseo.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out fecha))
            {
                error = "La fecha del paseo no es válida.";
                return null;
            }

            var paseador = this.repositorioPaseadores.ObtenerUno(criterioSolicitudPaseo.WalkerId);
            if (paseador == null)
            {
                error = "El paseador seleccionado no existe.";
                return null;
            }

            //Se valida contra la agenda actual del paseador, por si el horario se ocupo mientras se reservaba
            var horariosDisponibles = this.servicioPaseadores.ObtenerHorariosDisponibles(paseador.Id, fecha);
            if (!horariosDisponibles.Contains(criterioSolicitudPaseo.TimeFrom))
            {
                error = "El paseador ya no está disponible en el día y horario seleccionados.";
                return null;
            }

            var idsMascotas = criterioSolicitudPaseo.PetIds.Distinct().ToList();
            var mascotas = this.repositorioMascotas.ObtenerTodos().Where(x => idsMascotas.Contains(x.Id)).ToList();
            if (mascotas.Count != idsMascotas.Count)
            {
                error = "Alguna de las mascotas seleccionadas no existe.";
                return null;
            }

            //Direccion de retiro: la elegida para este paseo o, si no se informa, el domicilio del cliente.
            //Siempre se guarda una copia, asi el paseo conserva su direccion aunque el cliente cambie su perfil.
            var retiro = ResolverDireccionDeRetiro(criterioSolicitudPaseo, mascotas, out error);
            if (retiro == null)
            {
                return null;
            }

            //El retiro tiene que quedar dentro de la zona de trabajo del paseador (se controla si se conocen las coordenadas de ambos)
            Double latitudPaseador, longitudPaseador, latitudRetiro, longitudRetiro;
            if (paseador.ServiceRadiusKm > 0 &&
                Geografia.IntentarLeerCoordenadas(paseador.Latitude, paseador.Longitude, out latitudPaseador, out longitudPaseador) &&
                Geografia.IntentarLeerCoordenadas(retiro.Latitude, retiro.Longitude, out latitudRetiro, out longitudRetiro))
            {
                var distancia = Geografia.DistanciaEnKilometros(latitudPaseador, longitudPaseador, latitudRetiro, longitudRetiro);
                if (distancia > paseador.ServiceRadiusKm)
                {
                    error = "La dirección de retiro queda a " + Math.Round(distancia, 1).ToString("0.#", CultureInfo.GetCultureInfo("es-AR")) + " km, fuera de la zona de trabajo de " + paseador.FirstName +
                            " (hasta " + paseador.ServiceRadiusKm.ToString("0.#", CultureInfo.GetCultureInfo("es-AR")) + " km de su dirección de referencia).";
                    return null;
                }
            }

            var metodoPago = String.IsNullOrWhiteSpace(criterioSolicitudPaseo.PaymentMethod) ? PaymentMethods.Cash : criterioSolicitudPaseo.PaymentMethod;
            if (metodoPago != PaymentMethods.Cash && metodoPago != PaymentMethods.MercadoPago)
            {
                error = "El método de pago no es válido.";
                return null;
            }
            if (metodoPago == PaymentMethods.MercadoPago && !this.proveedorTokens.EstaVinculada(paseador.Id))
            {
                error = "Este paseador todavía no vinculó su cuenta de Mercado Pago. Elegí pagar en efectivo.";
                return null;
            }

            var horaDesde = criterioSolicitudPaseo.TimeFrom;
            var paseosEnLaMismaHora = this.repositorioPaseos.ObtenerTodos().
                                                       Where(x => x.Date == fecha && x.TimeFrom == horaDesde && x.Status != WalkStatus.Cancelled).
                                                       ToList();

            var mascotaOcupada = mascotas.FirstOrDefault(mascota => paseosEnLaMismaHora.Any(paseo => paseo.PetId == mascota.Id));
            if (mascotaOcupada != null)
            {
                error = mascotaOcupada.Name + " ya tiene reservado un paseo a la misma hora y día seleccionados.";
                return null;
            }

            var mascotasYaReservadas = paseosEnLaMismaHora.Count(x => x.WalkerId == paseador.Id);
            if (mascotasYaReservadas + mascotas.Count > ServicioPaseadores.MaximoMascotasPorPaseo)
            {
                error = "El paseador solo puede llevar hasta " + ServicioPaseadores.MaximoMascotasPorPaseo +
                        " mascotas a la vez en ese horario.";
                return null;
            }

            //Los paseos de una misma reserva (uno por mascota) comparten el BookingCode y arrancan esperando la respuesta del paseador
            var codigoReserva = Guid.NewGuid();
            var paseos = new List<Walk>();
            foreach (var mascota in mascotas)
            {
                var paseo = new Walk
                {
                    Status = WalkStatus.Pending,
                    BookingCode = codigoReserva,
                    PaymentMethod = metodoPago,
                    PaymentStatus = PaymentStatuses.Pending,
                    WalkerId = paseador.Id,
                    PetId = mascota.Id,
                    PriceId = paseador.PriceId,
                    Date = fecha,
                    TimeFrom = horaDesde,
                    Details = criterioSolicitudPaseo.Details,
                    Confirmed = false,
                    PickupStreetName = retiro.StreetName,
                    PickupStreetNumber = retiro.StreetNumber,
                    PickupCityId = retiro.CityId,
                    PickupLatitude = retiro.Latitude,
                    PickupLongitude = retiro.Longitude
                };
                this.repositorioPaseos.Agregar(paseo);
                paseos.Add(paseo);
            }

            this.unidadDeTrabajo.GuardarCambios();

            this.servicioNotificaciones.ReservaSolicitada(paseos);

            return paseos.Select(paseo => new WalkDto
            {
                Id = paseo.Id,
                Date = paseo.Date,
                TimeFrom = paseo.TimeFrom,
                Details = paseo.Details,
                PetName = mascotas.First(mascota => mascota.Id == paseo.PetId).Name
            }).ToList();
        }

        private class DireccionDeRetiro
        {
            public String StreetName { get; set; }
            public Int64? StreetNumber { get; set; }
            public Int64? CityId { get; set; }
            public String Latitude { get; set; }
            public String Longitude { get; set; }
        }

        private DireccionDeRetiro ResolverDireccionDeRetiro(WalkRequestCriteria criterioSolicitudPaseo, List<Pet> mascotas, out String error)
        {
            error = null;

            if (!String.IsNullOrWhiteSpace(criterioSolicitudPaseo.PickupStreetName))
            {
                var calle = criterioSolicitudPaseo.PickupStreetName.Trim();
                if (calle.Length > 50 ||
                    !criterioSolicitudPaseo.PickupStreetNumber.HasValue || criterioSolicitudPaseo.PickupStreetNumber.Value <= 0 ||
                    !criterioSolicitudPaseo.PickupCityId.HasValue ||
                    this.repositorioCiudades.ObtenerUno(criterioSolicitudPaseo.PickupCityId.Value) == null)
                {
                    error = "La dirección de retiro no es válida. Elegila de la lista de sugerencias.";
                    return null;
                }

                return new DireccionDeRetiro
                {
                    StreetName = calle,
                    StreetNumber = criterioSolicitudPaseo.PickupStreetNumber,
                    CityId = criterioSolicitudPaseo.PickupCityId,
                    Latitude = criterioSolicitudPaseo.PickupLatitude,
                    Longitude = criterioSolicitudPaseo.PickupLongitude
                };
            }

            var idsClientes = mascotas.Select(x => x.CustomerId).Distinct().ToList();
            var cliente = idsClientes.Count == 1 ? this.repositorioClientes.ObtenerUno(idsClientes[0]) : null;
            if (cliente == null)
            {
                error = "Las mascotas del paseo deben ser de un mismo cliente.";
                return null;
            }

            return new DireccionDeRetiro
            {
                StreetName = cliente.StreetName,
                StreetNumber = cliente.StreetNumber,
                CityId = cliente.CityId,
                Latitude = cliente.Latitude,
                Longitude = cliente.Longitude
            };
        }

        /* ---------- Reservas: confirmar, cancelar y vencer ---------- */

        public List<BookingDto> ObtenerReservasDelPaseador(Int64 idPaseador)
        {
            VencerPaseosPendientes();

            //Ademas de los paseos de hoy en adelante se devuelven los que ya pasaron pero todavia tienen algo pendiente
            //(darlos por finalizados o confirmar que se cobraron) y los cobrados en la ultima semana
            var hoy = DateTime.Now.Date;
            var limiteRecibidos = hoy.AddDays(-7);
            var paseos = IncluirDatosDeReserva().
                        Where(x => x.WalkerId == idPaseador && x.Status != WalkStatus.Cancelled &&
                                   (x.Date >= hoy ||
                                    (x.Status == WalkStatus.Confirmed &&
                                     (x.ReceivedAt == null || x.ReceivedAt >= limiteRecibidos)))).
                        ToList();

            return ArmarReservas(paseos).
                   OrderBy(x => x.Date).
                   ThenBy(x => x.TimeFrom).
                   ToList();
        }

        public List<BookingDto> ObtenerReservasDelCliente(Int64 idCliente)
        {
            VencerPaseosPendientes();

            var paseos = IncluirDatosDeReserva().
                        Where(x => x.Pet.CustomerId == idCliente).
                        ToList();

            return ArmarReservas(paseos).
                   OrderByDescending(x => x.Date).
                   ThenByDescending(x => x.TimeFrom).
                   ToList();
        }

        public Boolean Confirmar(BookingActionCriteria criterioAccionReserva, out String error)
        {
            error = null;
            VencerPaseosPendientes();

            var paseos = BuscarReserva(criterioAccionReserva.BookingKey);
            if (paseos.Count == 0 || paseos[0].WalkerId != criterioAccionReserva.ActorId)
            {
                error = "La reserva no existe.";
                return false;
            }

            if (paseos.Any(x => x.Status != WalkStatus.Pending))
            {
                error = paseos[0].Status == WalkStatus.Cancelled
                    ? "Esta reserva ya fue cancelada."
                    : "Esta reserva ya estaba confirmada.";
                return false;
            }

            paseos.ForEach(x => CambiarEstado(x, WalkStatus.Confirmed, null));
            this.unidadDeTrabajo.GuardarCambios();

            this.servicioNotificaciones.ReservaConfirmada(paseos);
            return true;
        }

        public Boolean Cancelar(BookingActionCriteria criterioAccionReserva, out String error)
        {
            error = null;
            VencerPaseosPendientes();

            var actor = criterioAccionReserva.Actor;
            if (actor != WalkCancelledBy.Walker && actor != WalkCancelledBy.Customer)
            {
                error = "No se puede cancelar la reserva.";
                return false;
            }

            var paseos = BuscarReserva(criterioAccionReserva.BookingKey);
            var esDuenio = paseos.Count > 0 &&
                          (actor == WalkCancelledBy.Walker
                              ? paseos[0].WalkerId == criterioAccionReserva.ActorId
                              : paseos[0].Pet.CustomerId == criterioAccionReserva.ActorId);
            if (!esDuenio)
            {
                error = "La reserva no existe.";
                return false;
            }

            if (paseos.All(x => x.Status == WalkStatus.Cancelled))
            {
                error = "Esta reserva ya estaba cancelada.";
                return false;
            }

            if (InicioDe(paseos[0]) <= DateTime.Now)
            {
                error = "El paseo ya comenzó y no se puede cancelar.";
                return false;
            }

            paseos.Where(x => x.Status != WalkStatus.Cancelled).ToList().ForEach(x => CambiarEstado(x, WalkStatus.Cancelled, actor));
            this.unidadDeTrabajo.GuardarCambios();

            this.servicioNotificaciones.ReservaCancelada(paseos, actor);
            return true;
        }

        //El paseador da por finalizado el paseo (ya devolvio a la mascota). Desde ese momento el cliente puede pagarlo.
        public Boolean Finalizar(BookingActionCriteria criterioAccionReserva, out String error)
        {
            error = null;
            VencerPaseosPendientes();

            var paseos = BuscarReserva(criterioAccionReserva.BookingKey);
            if (paseos.Count == 0 || paseos[0].WalkerId != criterioAccionReserva.ActorId)
            {
                error = "La reserva no existe.";
                return false;
            }

            if (paseos.Any(x => x.Status != WalkStatus.Confirmed))
            {
                error = paseos.All(x => x.Status == WalkStatus.Cancelled)
                    ? "Esta reserva fue cancelada."
                    : "Primero tenés que confirmar la reserva.";
                return false;
            }

            if (paseos.Any(x => x.FinishedAt.HasValue))
            {
                error = "Este paseo ya estaba finalizado.";
                return false;
            }

            if (InicioDe(paseos[0]) > DateTime.Now)
            {
                error = "El paseo todavía no empezó.";
                return false;
            }

            paseos.ForEach(x => x.FinishedAt = DateTime.Now);
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        //El paseador confirma que recibio el pago, ya sea en efectivo o con Mercado Pago. Con eso la reserva queda cerrada.
        public Boolean ConfirmarRecibido(BookingActionCriteria criterioAccionReserva, out String error)
        {
            error = null;

            var paseos = BuscarReserva(criterioAccionReserva.BookingKey);
            if (paseos.Count == 0 || paseos[0].WalkerId != criterioAccionReserva.ActorId)
            {
                error = "La reserva no existe.";
                return false;
            }

            if (paseos.Any(x => x.Status != WalkStatus.Confirmed || !x.FinishedAt.HasValue))
            {
                error = "Primero tenés que dar el paseo por finalizado.";
                return false;
            }

            if (paseos.All(x => x.PaymentStatus == PaymentStatuses.Received))
            {
                error = "Ya confirmaste que recibiste este pago.";
                return false;
            }

            //Con Mercado Pago el pago tiene que figurar como pagado; en efectivo se confirma directamente
            if (paseos[0].PaymentMethod == PaymentMethods.MercadoPago && paseos.Any(x => x.PaymentStatus != PaymentStatuses.Paid))
            {
                error = "El cliente todavía no pagó con Mercado Pago.";
                return false;
            }

            paseos.ForEach(x =>
            {
                x.PaymentStatus = PaymentStatuses.Received;
                x.ReceivedAt = DateTime.Now;
            });
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        //Un pedido que el paseador no respondio antes de la hora del paseo se cancela solo
        private void VencerPaseosPendientes()
        {
            var ahora = DateTime.Now;
            var paseosPendientes = this.repositorioPaseos.ObtenerTodos().
                                                    Where(x => x.Status == WalkStatus.Pending && x.Date <= ahora.Date).
                                                    ToList();

            var vencidos = paseosPendientes.Where(x => InicioDe(x) <= ahora).ToList();
            if (vencidos.Count == 0)
            {
                return;
            }

            vencidos.ForEach(x => CambiarEstado(x, WalkStatus.Cancelled, WalkCancelledBy.System));
            this.unidadDeTrabajo.GuardarCambios();

            this.servicioNotificaciones.ReservaCancelada(vencidos, WalkCancelledBy.System);
        }

        private static DateTime InicioDe(Walk paseo)
        {
            return AyudanteReservas.InicioDe(paseo);
        }

        private static void CambiarEstado(Walk paseo, String estado, String canceladoPor)
        {
            paseo.Status = estado;
            paseo.Confirmed = estado == WalkStatus.Confirmed;
            paseo.CancelledBy = canceladoPor;
            paseo.StatusChangedAt = DateTime.Now;
        }

        private static String ClaveDeReserva(Walk paseo)
        {
            return AyudanteReservas.ClaveDe(paseo);
        }

        private List<Walk> BuscarReserva(String claveReserva)
        {
            return AyudanteReservas.Buscar(IncluirDatosDeReserva(), claveReserva);
        }

        private IQueryable<Walk> IncluirDatosDeReserva()
        {
            return this.repositorioPaseos.TodosConIncluidos(x => x.Pet, x => x.Pet.Customer,
                                                     x => x.Pet.Customer.City, x => x.Pet.Customer.City.Province,
                                                     x => x.PickupCity, x => x.PickupCity.Province,
                                                     x => x.Walker, x => x.Price);
        }

        private List<BookingDto> ArmarReservas(List<Walk> paseos)
        {
            //Valoraciones ya hechas por el cliente para estas reservas
            var clavesReserva = paseos.Select(ClaveDeReserva).Distinct().ToList();
            var valoradasPorClave = this.repositorioValoraciones.ObtenerTodos().
                                                     Where(x => clavesReserva.Contains(x.BookingKey)).
                                                     ToList().
                                                     ToDictionary(x => x.BookingKey, x => (Int32)Math.Round(x.Score));

            return paseos.GroupBy(ClaveDeReserva).Select(group =>
            {
                var primero = group.First();
                var cliente = primero.Pet.Customer;
                var usaDireccionDeRetiro = primero.PickupStreetName != null;
                var precioPorMascota = primero.Price != null ? primero.Price.Amount : 0;

                string ubicacion;
                if (usaDireccionDeRetiro)
                {
                    ubicacion = primero.PickupStreetName + " " + primero.PickupStreetNumber + " - " +
                               (primero.PickupCity != null ? primero.PickupCity.Name + " - " + (primero.PickupCity.Province != null ? primero.PickupCity.Province.Name : "") : "");
                }
                else
                {
                    ubicacion = cliente.StreetName + " " + cliente.StreetNumber + " - " +
                               (cliente.City != null ? cliente.City.Name + " - " + (cliente.City.Province != null ? cliente.City.Province.Name : "") : "");
                }

                return new BookingDto
                {
                    BookingKey = group.Key,
                    WalkerId = primero.WalkerId,
                    WalkerName = primero.Walker != null ? primero.Walker.FirstName + " " + primero.Walker.LastName : null,
                    WalkerProfileImage = primero.Walker != null ? primero.Walker.ProfileImage : null,
                    WalkerPayoutAccount = primero.Walker != null ? primero.Walker.PayoutAccount : null,
                    CustomerId = cliente.Id,
                    CustomerFullName = cliente.FirstName + " " + cliente.LastName,
                    Date = primero.Date,
                    TimeFrom = primero.TimeFrom,
                    Status = primero.Status,
                    CancelledBy = primero.CancelledBy,
                    PaymentMethod = primero.PaymentMethod,
                    PaymentStatus = primero.PaymentStatus,
                    FinishedAt = primero.FinishedAt,
                    PaidAt = primero.PaidAt,
                    ReceivedAt = primero.ReceivedAt,
                    RatingStars = valoradasPorClave.ContainsKey(group.Key) ? valoradasPorClave[group.Key] : (Int32?)null,
                    Details = primero.Details,
                    Location = ubicacion,
                    Latitude = usaDireccionDeRetiro ? primero.PickupLatitude : cliente.Latitude,
                    Longitude = usaDireccionDeRetiro ? primero.PickupLongitude : cliente.Longitude,
                    PricePerPet = precioPorMascota,
                    Total = precioPorMascota * group.Count(),
                    Pets = group.Select(x => new BookingPetDto { Id = x.Pet.Id, Name = x.Pet.Name, ProfileImage = x.Pet.ProfileImage }).ToList()
                };
            }).ToList();
        }

        public List<WalkDto> ObtenerTodos()
        {
            var paseos = this.repositorioPaseos.ObtenerTodos().ToList();
            var paseosDto = Mapper.Map<List<Walk>, List<WalkDto>>(paseos);
            return paseosDto;
        }

        public List<WalkDto> ObtenerTodosPorIdPaseador(Int64 idPaseador)
        {
            var paseos = this.repositorioPaseos.ObtenerTodos().Where(x => x.WalkerId == idPaseador).ToList();
            var paseosDto = Mapper.Map<List<Walk>, List<WalkDto>>(paseos);
            return paseosDto;
        }

        public List<WalkDto> ObtenerTodosDelDiaActual(Int64 idPaseador)
        {
            var fechaYHoraActual = DateTime.Now;
            var horaActual = fechaYHoraActual.Hour;
            var minutoActual = fechaYHoraActual.Minute;
            var fechaActual = fechaYHoraActual.Date;

            var paseos = this.repositorioPaseos.TodosConIncluidos(x => x.Pet, x => x.Pet.Customer,
                                                          x => x.Pet.Customer.City.Province,
                                                          x => x.PickupCity, x => x.PickupCity.Province).
                                             Where(x => x.WalkerId == idPaseador &&
                                                        x.Date == fechaActual && x.Confirmed == false).
                                             ToList();

            var paseosDisponibles = new List<Walk>();
            foreach (var paseo in paseos)
            {
                var horaDesde = paseo.TimeFrom;
                var hora = Convert.ToInt32(horaDesde.Split(':')[0]);
                if (horaActual >= hora)
                {
                    //Se setea como vencido para diferenciarlos en la vista web
                    paseo.IsExpired = true;
                    paseosDisponibles.Add(paseo);

                }
                else
                {
                    paseosDisponibles.Add(paseo);
                }
            }

            var paseosDto = Mapper.Map<List<Walk>, List<WalkDto>>(paseosDisponibles);
            return paseosDto;
        }

        public WalkDto ValidarMascotasEnPaseos(AvailableWalkersCriteria criterioPaseadoresDisponibles)
        {
            var servicioDiaDeLaSemana = new ServicioDiaDeLaSemana();
            var fecha = DateTime.Parse(criterioPaseadoresDisponibles.Date).Date;
            var diaDeLaSemana = servicioDiaDeLaSemana.ObtenerDiaDeLaSemana(fecha.DayOfWeek.ToString());
            var horaDesde = Convert.ToInt64(criterioPaseadoresDisponibles.TimeFrom.Split(':')[0]);

            var paseos = this.repositorioPaseos.TodosConIncluidos(x=>x.Pet).Where(x => x.Date == fecha && x.Status != WalkStatus.Cancelled &&
                                                               x.TimeFrom == criterioPaseadoresDisponibles.TimeFrom);
            var paseoExistenteConMascotaDto = new WalkDto();
            if (paseos.Count() > 0)
            {
                foreach (var mascotaDto in criterioPaseadoresDisponibles.SelectedPets)
                {
                    var paseoExistenteConMascota = paseos.Where(x => x.PetId == mascotaDto.Id).FirstOrDefault();
                    if (paseoExistenteConMascota != null)
                    {
                        paseoExistenteConMascotaDto = Mapper.Map<Walk, WalkDto>(paseoExistenteConMascota);
                        return paseoExistenteConMascotaDto;
                    }
                }
            }
            else
            {
                return paseoExistenteConMascotaDto;
            }
            return paseoExistenteConMascotaDto;
        }
    }
}
