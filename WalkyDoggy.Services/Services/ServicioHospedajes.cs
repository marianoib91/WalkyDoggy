using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    public class ServicioHospedajes : IServicioHospedajes
    {
        public const Int32 NochesMaximas = 30;
        public const Int32 PerrosMaximosPorCuidador = 6;
        public const Decimal PrecioMaximoPorNoche = 1000000m;
        public const Int32 LargoMaximoTexto = 500;

        private const String PrefijoClave = "h";

        private readonly IRepositorioEntidadBase<Stay> repositorioHospedajes;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;
        private readonly IRepositorioEntidadBase<Pet> repositorioMascotas;
        private readonly IRepositorioEntidadBase<Ranking> repositorioValoraciones;
        private readonly IRepositorioEntidadBase<PetReview> repositorioResenas;
        private readonly IProveedorTokensVendedor proveedorTokens;
        private readonly IServicioMensajes servicioMensajes;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;

        public ServicioHospedajes(IRepositorioEntidadBase<Stay> repositorioHospedajes,
                                  IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                  IRepositorioEntidadBase<Customer> repositorioClientes,
                                  IRepositorioEntidadBase<Pet> repositorioMascotas,
                                  IRepositorioEntidadBase<Ranking> repositorioValoraciones,
                                  IRepositorioEntidadBase<PetReview> repositorioResenas,
                                  IProveedorTokensVendedor proveedorTokens,
                                  IServicioMensajes servicioMensajes,
                                  IUnidadDeTrabajo unidadDeTrabajo)
        {
            this.repositorioHospedajes = repositorioHospedajes;
            this.repositorioPaseadores = repositorioPaseadores;
            this.repositorioClientes = repositorioClientes;
            this.repositorioMascotas = repositorioMascotas;
            this.repositorioValoraciones = repositorioValoraciones;
            this.repositorioResenas = repositorioResenas;
            this.proveedorTokens = proveedorTokens;
            this.servicioMensajes = servicioMensajes;
            this.unidadDeTrabajo = unidadDeTrabajo;
        }

        /* ---------- Oferta del cuidador ---------- */

        public BoardingOfferDto ObtenerOferta(Int64 idPaseador)
        {
            var paseador = this.repositorioPaseadores.ObtenerTodos().FirstOrDefault(x => x.Id == idPaseador);
            if (paseador == null)
            {
                return null;
            }

            return new BoardingOfferDto
            {
                WalkerId = paseador.Id,
                Enabled = paseador.BoardingEnabled,
                PricePerNight = paseador.BoardingPricePerNight,
                MaxDogs = paseador.BoardingMaxDogs < 1 ? 1 : paseador.BoardingMaxDogs,
                Description = paseador.BoardingDescription
            };
        }

        public Boolean GuardarOferta(BoardingOfferDto oferta, out String error)
        {
            error = null;

            var paseador = oferta == null ? null : this.repositorioPaseadores.ObtenerTodos().FirstOrDefault(x => x.Id == oferta.WalkerId);
            if (paseador == null)
            {
                error = "El paseador no existe.";
                return false;
            }

            var descripcion = (oferta.Description ?? String.Empty).Trim();
            if (descripcion.Length > LargoMaximoTexto)
            {
                error = "La descripción puede tener hasta " + LargoMaximoTexto + " caracteres.";
                return false;
            }
            if (oferta.MaxDogs < 1 || oferta.MaxDogs > PerrosMaximosPorCuidador)
            {
                error = "Indicá cuántos perros cuidás a la vez (de 1 a " + PerrosMaximosPorCuidador + ").";
                return false;
            }

            if (oferta.Enabled)
            {
                if (!oferta.PricePerNight.HasValue || oferta.PricePerNight.Value <= 0 || oferta.PricePerNight.Value > PrecioMaximoPorNoche)
                {
                    error = "Indicá el precio por noche y por perro.";
                    return false;
                }
                if (descripcion.Length < 10)
                {
                    error = "Contá cómo es el lugar donde cuidás a los perros (al menos 10 caracteres).";
                    return false;
                }
            }

            paseador.BoardingEnabled = oferta.Enabled;
            paseador.BoardingPricePerNight = oferta.PricePerNight.HasValue ? (Decimal?)Math.Round(oferta.PricePerNight.Value, 2) : null;
            paseador.BoardingMaxDogs = oferta.MaxDogs;
            paseador.BoardingDescription = descripcion.Length == 0 ? null : descripcion;
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        /* ---------- Busqueda ---------- */

        public List<BoardingCarerDto> Buscar(String entrada, String salida, Int32 perros, String latitud, String longitud, out String error)
        {
            DateTime desde, hasta;
            if (!LeerFechas(entrada, salida, out desde, out hasta, out error))
            {
                return null;
            }
            if (perros < 1 || perros > PerrosMaximosPorCuidador)
            {
                error = "Elegí al menos un perro (hasta " + PerrosMaximosPorCuidador + ").";
                return null;
            }

            VencerPendientes();

            var noches = (Int32)(hasta - desde).TotalDays;
            var cuidadores = this.repositorioPaseadores.TodosConIncluidos(x => x.City).
                Where(x => x.BoardingEnabled && x.BoardingPricePerNight.HasValue).
                ToList();

            var idsCuidadores = cuidadores.Select(x => x.Id).ToList();
            var hospedajes = this.repositorioHospedajes.ObtenerTodos().
                Where(x => idsCuidadores.Contains(x.WalkerId) && x.Status != WalkStatus.Cancelled && x.CheckIn < hasta && x.CheckOut > desde).
                Select(x => new { x.WalkerId, x.CheckIn, x.CheckOut, x.DogsCount }).
                ToList();

            var valoraciones = this.repositorioValoraciones.ObtenerTodos().
                Where(x => !x.Hidden && idsCuidadores.Contains(x.WalkerId)).
                Select(x => new { x.WalkerId, x.Score }).
                ToList().
                GroupBy(x => x.WalkerId).
                ToDictionary(g => g.Key, g => new { Promedio = g.Average(v => v.Score), Cantidad = g.Count() });

            Double latitudCliente, longitudCliente;
            var conUbicacion = Geografia.IntentarLeerCoordenadas(latitud, longitud, out latitudCliente, out longitudCliente);

            var resultado = new List<BoardingCarerDto>();
            foreach (var cuidador in cuidadores)
            {
                var libres = cuidador.BoardingMaxDogs;
                for (var noche = desde; noche < hasta; noche = noche.AddDays(1))
                {
                    var ocupados = hospedajes.Where(x => x.WalkerId == cuidador.Id && x.CheckIn <= noche && x.CheckOut > noche).Sum(x => x.DogsCount);
                    libres = Math.Min(libres, cuidador.BoardingMaxDogs - ocupados);
                }
                if (libres < perros)
                {
                    continue;
                }

                Double latitudCuidador, longitudCuidador;
                Double? distancia = null;
                if (conUbicacion && Geografia.IntentarLeerCoordenadas(cuidador.Latitude, cuidador.Longitude, out latitudCuidador, out longitudCuidador))
                {
                    distancia = Math.Round(Geografia.DistanciaEnKilometros(latitudCliente, longitudCliente, latitudCuidador, longitudCuidador), 1);
                }

                var precio = cuidador.BoardingPricePerNight.Value;
                var valoracion = valoraciones.ContainsKey(cuidador.Id) ? valoraciones[cuidador.Id] : null;
                resultado.Add(new BoardingCarerDto
                {
                    WalkerId = cuidador.Id,
                    Name = (cuidador.FirstName + " " + cuidador.LastName).Trim(),
                    ProfileImage = cuidador.ProfileImage,
                    CityName = cuidador.City != null ? cuidador.City.Name : null,
                    About = cuidador.Description,
                    BoardingDescription = cuidador.BoardingDescription,
                    PricePerNight = precio,
                    MaxDogs = cuidador.BoardingMaxDogs,
                    FreeSpots = libres,
                    Nights = noches,
                    Dogs = perros,
                    Total = noches * perros * precio,
                    DistanceKm = distancia,
                    Rating = valoracion != null ? Math.Round(valoracion.Promedio, 1) : 0,
                    RatingCount = valoracion != null ? valoracion.Cantidad : 0,
                    MercadoPagoLinked = this.proveedorTokens.EstaVinculada(cuidador.Id)
                });
            }

            return resultado.
                OrderBy(x => x.DistanceKm.HasValue ? 0 : 1).
                ThenBy(x => x.DistanceKm).
                ThenByDescending(x => x.Rating).
                ToList();
        }

        /* ---------- Solicitud ---------- */

        public StayDto Solicitar(StayRequestDto solicitud, out String error)
        {
            error = null;

            if (solicitud == null || solicitud.PetIds == null || solicitud.PetIds.Count == 0)
            {
                error = "Elegí al menos un perro.";
                return null;
            }

            DateTime desde, hasta;
            if (!LeerFechas(solicitud.CheckIn, solicitud.CheckOut, out desde, out hasta, out error))
            {
                return null;
            }

            var detalles = (solicitud.Details ?? String.Empty).Trim();
            if (detalles.Length > LargoMaximoTexto)
            {
                error = "Las indicaciones pueden tener hasta " + LargoMaximoTexto + " caracteres.";
                return null;
            }

            var cliente = this.repositorioClientes.ObtenerTodos().FirstOrDefault(x => x.Id == solicitud.CustomerId);
            var cuidador = this.repositorioPaseadores.ObtenerTodos().FirstOrDefault(x => x.Id == solicitud.WalkerId);
            if (cliente == null || cuidador == null || !cuidador.BoardingEnabled || !cuidador.BoardingPricePerNight.HasValue)
            {
                error = "Este cuidador no ofrece hospedaje.";
                return null;
            }

            //Como se va a pagar: efectivo o Mercado Pago (este solo si el cuidador vinculo su cuenta), igual que en los paseos
            var metodoPago = String.IsNullOrWhiteSpace(solicitud.PaymentMethod) ? PaymentMethods.Cash : solicitud.PaymentMethod;
            if (metodoPago != PaymentMethods.Cash && metodoPago != PaymentMethods.MercadoPago)
            {
                error = "Elegí cómo vas a pagar: efectivo o Mercado Pago.";
                return null;
            }
            if (metodoPago == PaymentMethods.MercadoPago && !this.proveedorTokens.EstaVinculada(cuidador.Id))
            {
                error = "Este cuidador todavía no vinculó su cuenta de Mercado Pago: elegí pagar en efectivo.";
                return null;
            }

            var idsMascotas = solicitud.PetIds.Distinct().ToList();
            var mascotas = this.repositorioMascotas.ObtenerTodos().Where(x => idsMascotas.Contains(x.Id) && x.CustomerId == cliente.Id).ToList();
            if (mascotas.Count != idsMascotas.Count)
            {
                error = "Alguna de las mascotas no es tuya.";
                return null;
            }
            if (idsMascotas.Count > PerrosMaximosPorCuidador)
            {
                error = "Podés llevar hasta " + PerrosMaximosPorCuidador + " perros por hospedaje.";
                return null;
            }

            VencerPendientes();

            //Un perro no puede estar en dos hospedajes a la vez
            var superpuesto = this.repositorioHospedajes.ObtenerTodos().
                Any(x => x.Status != WalkStatus.Cancelled && x.CheckIn < hasta && x.CheckOut > desde && x.Pets.Any(p => idsMascotas.Contains(p.PetId)));
            if (superpuesto)
            {
                error = "Alguno de esos perros ya tiene un hospedaje en esas fechas.";
                return null;
            }

            //Lugar en todas las noches
            var existentes = this.repositorioHospedajes.ObtenerTodos().
                Where(x => x.WalkerId == cuidador.Id && x.Status != WalkStatus.Cancelled && x.CheckIn < hasta && x.CheckOut > desde).
                Select(x => new { x.CheckIn, x.CheckOut, x.DogsCount }).
                ToList();
            for (var noche = desde; noche < hasta; noche = noche.AddDays(1))
            {
                var ocupados = existentes.Where(x => x.CheckIn <= noche && x.CheckOut > noche).Sum(x => x.DogsCount);
                if (ocupados + idsMascotas.Count > cuidador.BoardingMaxDogs)
                {
                    error = "El cuidador no tiene lugar para " + idsMascotas.Count + (idsMascotas.Count == 1 ? " perro" : " perros") + " la noche del " + noche.ToString("dd/MM/yyyy") + ".";
                    return null;
                }
            }

            var noches = (Int32)(hasta - desde).TotalDays;
            var precio = cuidador.BoardingPricePerNight.Value;
            var ahora = DateTime.Now;
            var hospedaje = new Stay
            {
                WalkerId = cuidador.Id,
                CustomerId = cliente.Id,
                CheckIn = desde,
                CheckOut = hasta,
                Nights = noches,
                DogsCount = idsMascotas.Count,
                PricePerNight = precio,
                Total = noches * idsMascotas.Count * precio,
                Details = detalles.Length == 0 ? null : detalles,
                Status = WalkStatus.Pending,
                PaymentMethod = metodoPago,
                PaymentStatus = PaymentStatuses.Pending,
                CreatedAt = ahora,
                StatusChangedAt = ahora,
                Pets = idsMascotas.Select(id => new StayPet { PetId = id }).ToList()
            };
            this.repositorioHospedajes.Agregar(hospedaje);
            this.unidadDeTrabajo.GuardarCambios();

            return ObtenerPorId(hospedaje.Id, "Customer");
        }

        /* ---------- Listas ---------- */

        public List<StayDto> ObtenerDelPaseador(Int64 idPaseador)
        {
            VencerPendientes();
            var hospedajes = IncluirDatos().Where(x => x.WalkerId == idPaseador).OrderByDescending(x => x.CheckIn).ThenByDescending(x => x.Id).ToList();
            return ArmarLista(hospedajes, "Walker");
        }

        public List<StayDto> ObtenerDelCliente(Int64 idCliente)
        {
            VencerPendientes();
            var hospedajes = IncluirDatos().Where(x => x.CustomerId == idCliente).OrderByDescending(x => x.CheckIn).ThenByDescending(x => x.Id).ToList();
            return ArmarLista(hospedajes, "Customer");
        }

        private StayDto ObtenerPorId(Int64 id, String rol)
        {
            return ArmarLista(IncluirDatos().Where(x => x.Id == id).ToList(), rol).FirstOrDefault();
        }

        private IQueryable<Stay> IncluirDatos()
        {
            return this.repositorioHospedajes.TodosConIncluidos(x => x.Walker, x => x.Customer, x => x.Pets.Select(p => p.Pet));
        }

        private List<StayDto> ArmarLista(List<Stay> hospedajes, String rol)
        {
            var claves = hospedajes.Select(x => ClaveDe(x)).ToList();
            var resumenes = this.servicioMensajes.ObtenerResumenes(claves, rol);

            //Lo que ya se valoro: las estrellas del cliente al cuidador y las mascotas que el cuidador ya reseño
            var estrellas = this.repositorioValoraciones.ObtenerTodos().
                Where(x => claves.Contains(x.BookingKey)).
                Select(x => new { x.BookingKey, x.Score }).
                ToList().
                ToDictionary(x => x.BookingKey, x => (Int32)Math.Round(x.Score));
            var resenados = new HashSet<String>(this.repositorioResenas.ObtenerTodos().
                Where(x => claves.Contains(x.BookingKey)).
                Select(x => x.BookingKey + "|" + x.PetId).
                ToList());

            return hospedajes.Select(x =>
            {
                var confirmado = x.Status == WalkStatus.Confirmed;
                var resumen = resumenes.ContainsKey(ClaveDe(x)) ? resumenes[ClaveDe(x)] : null;
                return new StayDto
                {
                    Id = x.Id,
                    BookingKey = ClaveDe(x),
                    Status = EstadoDe(x),
                    CancelledBy = x.CancelledBy,
                    WalkerId = x.WalkerId,
                    WalkerName = (x.Walker.FirstName + " " + x.Walker.LastName).Trim(),
                    WalkerImage = x.Walker.ProfileImage,
                    CustomerId = x.CustomerId,
                    CustomerName = (x.Customer.FirstName + " " + x.Customer.LastName).Trim(),
                    CustomerImage = x.Customer.ProfileImage,
                    WalkerPhone = confirmado ? x.Walker.Phone : null,
                    CustomerPhone = confirmado ? x.Customer.Phone : null,
                    Pets = x.Pets.Select(p => new StayPetDto
                    {
                        Id = p.PetId,
                        Name = p.Pet.Name,
                        ProfileImage = p.Pet.ProfileImage,
                        ReviewedByWalker = resenados.Contains(ClaveDe(x) + "|" + p.PetId)
                    }).ToList(),
                    CheckIn = x.CheckIn,
                    CheckOut = x.CheckOut,
                    Nights = x.Nights,
                    DogsCount = x.DogsCount,
                    PricePerNight = x.PricePerNight,
                    Total = x.Total,
                    Details = x.Details,
                    CreatedAt = x.CreatedAt,
                    StartedAt = x.StartedAt,
                    FinishedAt = x.FinishedAt,
                    ReceivedAt = x.ReceivedAt,
                    WalkerPayoutAccount = rol == "Customer" && x.FinishedAt.HasValue && x.Status != WalkStatus.Cancelled ? x.Walker.PayoutAccount : null,
                    PaymentMethod = x.PaymentMethod,
                    PaymentStatus = x.PaymentStatus,
                    RatingStars = estrellas.ContainsKey(ClaveDe(x)) ? (Int32?)estrellas[ClaveDe(x)] : null,
                    ChatMessages = resumen != null ? resumen.Total : 0,
                    ChatUnread = resumen != null ? resumen.NoLeidos : 0
                };
            }).ToList();
        }

        public static String ClaveDe(Stay hospedaje)
        {
            return PrefijoClave + hospedaje.Id;
        }

        //Lo que ve el administrador y las listas: se deduce de las fechas y el estado
        public static String EstadoDe(Stay hospedaje)
        {
            if (hospedaje.Status == WalkStatus.Cancelled) { return AdminWalkStatuses.Cancelled; }
            if (hospedaje.Status == WalkStatus.Pending) { return AdminWalkStatuses.Pending; }
            if (hospedaje.ReceivedAt.HasValue) { return AdminWalkStatuses.Collected; }
            if (hospedaje.FinishedAt.HasValue) { return AdminWalkStatuses.ToCollect; }
            if (hospedaje.StartedAt.HasValue) { return AdminWalkStatuses.InProgress; }
            return AdminWalkStatuses.Upcoming;
        }

        /* ---------- Acciones ---------- */

        public Boolean Confirmar(BookingActionCriteria accion, out String error)
        {
            var hospedaje = BuscarDelActor(accion, WalkCancelledBy.Walker, out error);
            if (hospedaje == null)
            {
                return false;
            }
            if (hospedaje.Status != WalkStatus.Pending)
            {
                error = hospedaje.Status == WalkStatus.Cancelled ? "El hospedaje fue cancelado." : "El hospedaje ya estaba confirmado.";
                return false;
            }

            hospedaje.Status = WalkStatus.Confirmed;
            hospedaje.StatusChangedAt = DateTime.Now;
            this.unidadDeTrabajo.GuardarCambios();

            this.servicioMensajes.AgregarDelSistema(ClaveDe(hospedaje), "El cuidador confirmó el hospedaje. Coordinen por acá cómo y a qué hora entregar a los perros.");
            return true;
        }

        public Boolean Cancelar(BookingActionCriteria accion, out String error)
        {
            error = null;
            var actor = accion == null ? null : accion.Actor;
            if (actor != WalkCancelledBy.Walker && actor != WalkCancelledBy.Customer)
            {
                error = "No se puede cancelar el hospedaje.";
                return false;
            }

            var hospedaje = BuscarDelActor(accion, actor, out error);
            if (hospedaje == null)
            {
                return false;
            }
            if (hospedaje.Status == WalkStatus.Cancelled)
            {
                error = "Este hospedaje ya estaba cancelado.";
                return false;
            }
            if (hospedaje.StartedAt.HasValue)
            {
                error = "El cuidado de los perros ya comenzó y no se puede cancelar.";
                return false;
            }

            var estabaConfirmado = hospedaje.Status == WalkStatus.Confirmed;
            hospedaje.Status = WalkStatus.Cancelled;
            hospedaje.CancelledBy = actor;
            hospedaje.StatusChangedAt = DateTime.Now;
            this.unidadDeTrabajo.GuardarCambios();

            if (estabaConfirmado)
            {
                this.servicioMensajes.AgregarDelSistema(ClaveDe(hospedaje), actor == WalkCancelledBy.Walker ? "El cuidador canceló el hospedaje." : "El cliente canceló el hospedaje.");
            }
            return true;
        }

        public Boolean Iniciar(BookingActionCriteria accion, out String error)
        {
            var hospedaje = BuscarDelActor(accion, WalkCancelledBy.Walker, out error);
            if (hospedaje == null)
            {
                return false;
            }
            if (hospedaje.Status != WalkStatus.Confirmed)
            {
                error = hospedaje.Status == WalkStatus.Cancelled ? "El hospedaje fue cancelado." : "Primero tenés que confirmar el hospedaje.";
                return false;
            }
            if (hospedaje.StartedAt.HasValue)
            {
                error = "Ya recibiste a los perros.";
                return false;
            }
            if (DateTime.Now.Date < hospedaje.CheckIn.Date)
            {
                error = "Podés recibir a los perros desde el día de ingreso (" + hospedaje.CheckIn.ToString("dd/MM/yyyy") + ").";
                return false;
            }

            hospedaje.StartedAt = DateTime.Now;
            this.unidadDeTrabajo.GuardarCambios();

            this.servicioMensajes.AgregarDelSistema(ClaveDe(hospedaje), "El cuidador recibió a los perros: comenzó el hospedaje.");
            return true;
        }

        public Boolean Finalizar(BookingActionCriteria accion, out String error)
        {
            var hospedaje = BuscarDelActor(accion, WalkCancelledBy.Walker, out error);
            if (hospedaje == null)
            {
                return false;
            }
            if (hospedaje.Status != WalkStatus.Confirmed || !hospedaje.StartedAt.HasValue)
            {
                error = "Primero tenés que recibir a los perros.";
                return false;
            }
            if (hospedaje.FinishedAt.HasValue)
            {
                error = "Ya devolviste a los perros.";
                return false;
            }

            hospedaje.FinishedAt = DateTime.Now;
            this.unidadDeTrabajo.GuardarCambios();

            this.servicioMensajes.AgregarDelSistema(ClaveDe(hospedaje), "El cuidador devolvió a los perros: terminó el hospedaje. Ya se puede pagar.");
            return true;
        }

        public Boolean ConfirmarRecibido(BookingActionCriteria accion, out String error)
        {
            var hospedaje = BuscarDelActor(accion, WalkCancelledBy.Walker, out error);
            if (hospedaje == null)
            {
                return false;
            }
            if (hospedaje.Status != WalkStatus.Confirmed || !hospedaje.FinishedAt.HasValue)
            {
                error = "Todavía no terminó el hospedaje.";
                return false;
            }
            if (hospedaje.ReceivedAt.HasValue)
            {
                error = "Ya confirmaste el cobro.";
                return false;
            }
            //Siempre lo confirma el cuidador, con cualquier forma de pago: si Mercado Pago no funciona, el cobro se registra igual

            hospedaje.ReceivedAt = DateTime.Now;
            hospedaje.PaymentStatus = PaymentStatuses.Received;
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        /* ---------- Auxiliares ---------- */

        //El hospedaje de la clave "h<id>" si el actor es el cuidador o el cliente del hospedaje. rolRequerido: Walker (acciones del cuidador) o el mismo actor (cancelar).
        private Stay BuscarDelActor(BookingActionCriteria accion, String rolRequerido, out String error)
        {
            error = null;

            Int64 id;
            if (accion == null || String.IsNullOrEmpty(accion.BookingKey) || !accion.BookingKey.StartsWith(PrefijoClave) ||
                !Int64.TryParse(accion.BookingKey.Substring(PrefijoClave.Length), NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                error = "El hospedaje no existe.";
                return null;
            }

            VencerPendientes();

            var hospedaje = this.repositorioHospedajes.ObtenerTodos().FirstOrDefault(x => x.Id == id);
            var esDuenio = hospedaje != null && accion.Actor == rolRequerido &&
                           (rolRequerido == WalkCancelledBy.Walker ? hospedaje.WalkerId == accion.ActorId : hospedaje.CustomerId == accion.ActorId);
            if (!esDuenio)
            {
                error = "El hospedaje no existe.";
                return null;
            }

            return hospedaje;
        }

        //Un pedido que el cuidador no respondio antes del dia de ingreso se cancela solo
        private void VencerPendientes()
        {
            var hoy = DateTime.Now.Date;
            var vencidos = this.repositorioHospedajes.ObtenerTodos().Where(x => x.Status == WalkStatus.Pending && x.CheckIn < hoy).ToList();
            if (vencidos.Count == 0)
            {
                return;
            }

            vencidos.ForEach(x =>
            {
                x.Status = WalkStatus.Cancelled;
                x.CancelledBy = WalkCancelledBy.System;
                x.StatusChangedAt = DateTime.Now;
            });
            this.unidadDeTrabajo.GuardarCambios();
        }

        private static Boolean LeerFechas(String entrada, String salida, out DateTime desde, out DateTime hasta, out String error)
        {
            error = null;
            desde = DateTime.MinValue;
            hasta = DateTime.MinValue;

            if (!DateTime.TryParseExact(entrada, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out desde) ||
                !DateTime.TryParseExact(salida, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out hasta))
            {
                error = "Elegí la fecha de ingreso y la de salida.";
                return false;
            }
            if (desde < DateTime.Now.Date)
            {
                error = "La fecha de ingreso no puede ser anterior a hoy.";
                return false;
            }
            if (hasta <= desde)
            {
                error = "La salida tiene que ser posterior al ingreso (al menos una noche).";
                return false;
            }
            if ((hasta - desde).TotalDays > NochesMaximas)
            {
                error = "El hospedaje puede durar hasta " + NochesMaximas + " noches.";
                return false;
            }

            return true;
        }
    }
}
