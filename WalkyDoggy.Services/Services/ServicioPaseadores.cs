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
    public class ServicioPaseadores : ServicioEntidadBase<Walker, WalkerDto>, IServicioPaseadores
    {
        //Cuantas mascotas puede llevar a la vez un paseador (lo elige cada uno)
        public const Int32 MascotasMinimas = 1;
        public const Int32 MascotasMaximas = 5;

        //Al buscar solo por horario (sin dia), cuantos dias hacia adelante se mira, empezando por hoy
        public const Int32 DiasDeBusquedaPorHorario = 14;

        #region Variables
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;
        private readonly IRepositorioEntidadBase<WorkDay> repositorioJornadas;
        private readonly IRepositorioEntidadBase<Walk> repositorioPaseos;
        private readonly IRepositorioEntidadBase<Pet> repositorioMascotas;
        private readonly IRepositorioEntidadBase<Ranking> repositorioValoraciones;
        private readonly IRepositorioEntidadBase<Price> repositorioPrecios;
        private readonly IRepositorioEntidadBase<UserRole> repositorioRolesUsuario;
        private readonly IServicioEncriptacion servicioEncriptacion;
        private readonly IServicioMembresia servicioMembresia;
        private readonly IServicioCaracteristicas servicioCaracteristicas;
        private List<String> codigosActivos;
        #endregion

        public ServicioPaseadores(IRepositorioEntidadBase<Error> repositorioErrores,
                                IUnidadDeTrabajo unidadDeTrabajo,
                                IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                IRepositorioEntidadBase<Customer> repositorioClientes,
                                IRepositorioEntidadBase<WorkDay> repositorioJornadas,
                                IRepositorioEntidadBase<Walk> repositorioPaseos,
                                IRepositorioEntidadBase<Pet> repositorioMascotas,
                                IRepositorioEntidadBase<Ranking> repositorioValoraciones,
                                IRepositorioEntidadBase<Price> repositorioPrecios,
                                IRepositorioEntidadBase<UserRole> repositorioRolesUsuario,
                                IServicioEncriptacion servicioEncriptacion,
                                IServicioMembresia servicioMembresia,
                                IServicioCaracteristicas servicioCaracteristicas) :
            base(repositorioErrores, unidadDeTrabajo, repositorioPaseadores)
        {
            this.repositorioPaseadores = repositorioPaseadores;
            this.repositorioClientes = repositorioClientes;
            this.repositorioJornadas = repositorioJornadas;
            this.repositorioPaseos = repositorioPaseos;
            this.repositorioMascotas = repositorioMascotas;
            this.repositorioValoraciones = repositorioValoraciones;
            this.repositorioPrecios = repositorioPrecios;
            this.repositorioRolesUsuario = repositorioRolesUsuario;
            this.servicioEncriptacion = servicioEncriptacion;
            this.servicioMembresia = servicioMembresia;
            this.servicioCaracteristicas = servicioCaracteristicas;
        }

        //Las caracteristicas que hoy estan activas en el catalogo (se consultan una sola vez por pedido)
        private List<String> CodigosActivos()
        {
            return this.codigosActivos ?? (this.codigosActivos = this.servicioCaracteristicas.CodigosActivos());
        }

        //Las caracteristicas de una lista guardada que siguen activas: las dadas de baja no cuentan para el matching
        private List<String> LeerRasgos(String guardadas)
        {
            var activas = CodigosActivos();
            return PetTraits.Leer(guardadas).Where(x => activas.Contains(x)).ToList();
        }

        public WalkerDto Registrar(WalkerDto paseadorDto)
        {
            //Se registra el usuario correspondiente al paseador
            var usuarioDto = new UserDto();
            usuarioDto.Email = paseadorDto.Email;
            usuarioDto.CreatedDate = DateTime.Now;
            usuarioDto.IsLocked = false;
            usuarioDto.Password = paseadorDto.Password;
            var usuarioCreado = this.servicioMembresia.CrearUsuario(usuarioDto);

            //Se registra el rol del usuario en la tabla UserRoles
            var rolUsuario = new UserRole();
            rolUsuario.UserId = usuarioCreado.Id;
            rolUsuario.RoleId = Roles.Walker;
            this.repositorioRolesUsuario.Agregar(rolUsuario);

            //Se registra el paseador, con la tarifa por hora que el mismo eligio
            var paseador = Mapper.Map<WalkerDto, Walker>(paseadorDto);
            paseador.UserId = usuarioCreado.Id;
            paseador.PayoutAccount = String.IsNullOrWhiteSpace(paseadorDto.PayoutAccount) ? null : paseadorDto.PayoutAccount.Trim();
            AplicarTarifa(paseador, paseadorDto.Amount);
            this.repositorioPaseadores.Agregar(paseador);

            //Se guardan los registros en la base de datos y se devuelve el Walker creado
            this.unidadDeTrabajo.GuardarCambios();

            //Se devuelve el roleId y el userId para usarlo en la presentacion de las vistas de acuerdo al rol del usuario
            paseadorDto.RoleId = rolUsuario.RoleId;
            paseadorDto.UserId = paseador.UserId;

            return paseadorDto;
        }

        //Tarifa por hora que puede fijar un paseador (en pesos)
        public const Double TarifaMinima = 1;
        public const Double TarifaMaxima = 1000000;

        //Radio de trabajo que puede fijar un paseador, en kilometros
        public const Double RadioMinimoKm = 1;
        public const Double RadioMaximoKm = 50;

        //La tarifa la fija cada paseador. Se reutiliza la tabla de precios: si ya existe un precio con ese monto se usa,
        //y si no se crea. Los paseos ya reservados conservan el precio con el que se pidieron.
        private void AplicarTarifa(Walker paseador, Double monto)
        {
            var tarifa = Math.Round(monto, 2);

            var precio = this.repositorioPrecios.ObtenerTodos().Where(x => x.Amount == tarifa).FirstOrDefault();
            if (precio == null)
            {
                precio = new Price { Amount = tarifa };
                this.repositorioPrecios.Agregar(precio);
                paseador.Price = precio;
            }
            else
            {
                paseador.PriceId = precio.Id;
                paseador.Price = precio;
            }
        }

        //Completa el promedio y la cantidad de valoraciones de los paseadores
        private void AdjuntarValoraciones(List<WalkerDto> paseadoresDto)
        {
            if (paseadoresDto.Count == 0)
            {
                return;
            }

            var estadisticas = this.repositorioValoraciones.ObtenerTodos().
                                                Where(x => !x.Hidden).
                                                GroupBy(x => x.WalkerId).
                                                Select(g => new { WalkerId = g.Key, Count = g.Count(), Average = g.Average(x => x.Score) }).
                                                ToList();

            foreach (var paseadorDto in paseadoresDto)
            {
                var estadistica = estadisticas.FirstOrDefault(x => x.WalkerId == paseadorDto.Id);
                paseadorDto.RatingCount = estadistica != null ? estadistica.Count : 0;
                paseadorDto.AverageRating = estadistica != null ? Math.Round(estadistica.Average, 1) : (Double?)null;
            }
        }

        public WalkerDto ObtenerPorIdUsuario(Int64 idUsuario)
        {
            var paseador = this.repositorioPaseadores.TodosConIncluidos(x => x.City, x => x.City.Province, x => x.Price).Where(x => x.UserId == idUsuario).FirstOrDefault();
            var paseadorDto = Mapper.Map<Walker, WalkerDto>(paseador);
            if (paseadorDto != null)
            {
                AdjuntarValoraciones(new List<WalkerDto> { paseadorDto });
            }
            return paseadorDto;
        }

        public void Actualizar(WalkerDto paseadorDto)
        {
            //Se actualiza el paseador existente (y no una copia armada desde el DTO) para no pisar
            //con NULL las columnas que el DTO no trae, como los datos de la cuenta de Mercado Pago
            var paseador = this.repositorioPaseadores.ObtenerUno(paseadorDto.Id);
            if (paseador == null)
            {
                return;
            }

            var idPrecioAnterior = paseador.PriceId;
            var radioAnterior = paseador.ServiceRadiusKm;
            var cuentaAnterior = paseador.PayoutAccount;
            var cupoAnterior = paseador.MaxPetsAtOnce;
            Mapper.Map(paseadorDto, paseador);

            //Sin radio informado queda el que tenia; sin cuenta informada (null) tambien. Un texto vacio borra la cuenta.
            if (paseadorDto.ServiceRadiusKm <= 0)
            {
                paseador.ServiceRadiusKm = radioAnterior;
            }
            if (paseadorDto.MaxPetsAtOnce <= 0)
            {
                paseador.MaxPetsAtOnce = cupoAnterior;
            }
            if (paseadorDto.PayoutAccount == null)
            {
                paseador.PayoutAccount = cuentaAnterior;
            }
            else
            {
                paseador.PayoutAccount = String.IsNullOrWhiteSpace(paseadorDto.PayoutAccount) ? null : paseadorDto.PayoutAccount.Trim();
            }

            //La tarifa por hora se ajusta con el monto que informa el paseador (si no informa ninguno, queda la que tenia)
            if (paseadorDto.Amount > 0)
            {
                AplicarTarifa(paseador, paseadorDto.Amount);
            }
            else
            {
                paseador.PriceId = idPrecioAnterior;
            }

            this.unidadDeTrabajo.GuardarCambios();
        }

        public List<WalkerDto> ObtenerTodos()
        {
            var paseadores = this.repositorioPaseadores.TodosConIncluidos(x => x.City, x => x.Price, x => x.City.Province).ToList();
            var paseadoresDto = Mapper.Map<List<Walker>, List<WalkerDto>>(paseadores);
            AdjuntarValoraciones(paseadoresDto);
            paseadoresDto.ForEach(x => x.PayoutAccount = null);
            return paseadoresDto;
        }

        //Paseadores que trabajan en la direccion de retiro: los que la tienen dentro de su radio de trabajo, del mas cercano al mas lejano.
        //Siempre se respeta que el paseador lleve tantos perros a la vez como mascotas se eligieron (idsMascotas) y que tenga lugar para todas ellas.
        //Segun lo que se informe del dia y el horario preferidos:
        //  nada: todos los paseadores de la zona.
        //  solo la fecha: los que tienen algun horario libre ese dia.
        //  fecha y horario: los libres a esa hora ese dia.
        //  solo el horario: los que tienen lugar a esa hora algun dia de los proximos DiasDeBusquedaPorHorario.
        //Con las mascotas del cliente, cada horario trae cuantos perros parecidos lleva el paseador (matching entre mascotas).
        public List<WalkerDto> BuscarParaRetiro(Double latitud, Double longitud, DateTime? fecha, String horario, List<Int64> idsMascotas)
        {
            var paseadoresDto = ObtenerTodos();
            var encontrados = new List<WalkerDto>();
            var mascotasDelCliente = ObtenerMascotas(idsMascotas);
            var cantidadMascotas = Math.Max(1, mascotasDelCliente.Count);
            var buscaPorHorario = fecha.HasValue || !String.IsNullOrWhiteSpace(horario);

            foreach (var paseadorDto in paseadoresDto)
            {
                Double latitudPaseador, longitudPaseador;
                if (!Geografia.IntentarLeerCoordenadas(paseadorDto.Latitude, paseadorDto.Longitude, out latitudPaseador, out longitudPaseador))
                {
                    continue;
                }

                var distancia = Geografia.DistanciaEnKilometros(latitud, longitud, latitudPaseador, longitudPaseador);
                if (distancia > paseadorDto.ServiceRadiusKm)
                {
                    continue;
                }

                //Tiene que llevar a la vez tantos perros como mascotas se eligieron
                if (paseadorDto.MaxPetsAtOnce < cantidadMascotas)
                {
                    continue;
                }

                if (buscaPorHorario)
                {
                    var cupos = ObtenerCuposParaBusqueda(paseadorDto.Id, fecha, horario).
                                Where(x => x.FreeSpots >= cantidadMascotas).
                                ToList();
                    if (cupos.Count == 0)
                    {
                        continue;
                    }

                    paseadorDto.AvailableTimes = cupos;

                    if (mascotasDelCliente.Count > 0)
                    {
                        //Los lugares ocupados cambian de un dia a otro: el matching se calcula dia por dia
                        foreach (var delDia in cupos.GroupBy(x => x.Date))
                        {
                            AplicarCompatibilidad(paseadorDto.Id, DateTime.ParseExact(delDia.Key, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), delDia.ToList(), mascotasDelCliente);
                        }

                        //El paseador queda con los datos de su mejor horario: el que lleva al perro con mas caracteristicas en comun y, a igual cantidad, mas perros parecidos
                        var mejor = cupos.OrderByDescending(x => x.MatchScore).ThenByDescending(x => x.MatchingPets).First();
                        paseadorDto.MatchingPets = mejor.MatchingPets;
                        paseadorDto.MatchScore = mejor.MatchScore;
                        paseadorDto.SharedTraits = mejor.SharedTraits;
                        paseadorDto.BestMatch = mejor.BestMatch;
                    }
                }

                else if (mascotasDelCliente.Any(x => LeerRasgos(x.Traits).Count >= PetTraits.MinimoEnComun))
                {
                    //Sin dia ni horario elegidos tambien se puede ordenar por matching: se miran los proximos dias y el paseador trae
                    //solo los horarios en los que lleva perros parecidos (el resto de sus horarios no se informa)
                    var conCoincidencias = new List<AvailableTimeDto>();
                    for (var i = 0; i < DiasDeBusquedaPorHorario; i++)
                    {
                        var dia = DateTime.Now.Date.AddDays(i);
                        var delDia = ObtenerCupos(paseadorDto.Id, dia).Where(x => x.FreeSpots >= cantidadMascotas).ToList();
                        if (delDia.Count == 0)
                        {
                            continue;
                        }

                        AplicarCompatibilidad(paseadorDto.Id, dia, delDia, mascotasDelCliente);
                        conCoincidencias.AddRange(delDia.Where(x => x.MatchingPets > 0));
                    }

                    paseadorDto.AvailableTimes = conCoincidencias;
                    if (conCoincidencias.Count > 0)
                    {
                        var mejor = conCoincidencias.OrderByDescending(x => x.MatchScore).ThenByDescending(x => x.MatchingPets).First();
                        paseadorDto.MatchingPets = mejor.MatchingPets;
                        paseadorDto.MatchScore = mejor.MatchScore;
                        paseadorDto.SharedTraits = mejor.SharedTraits;
                        paseadorDto.BestMatch = mejor.BestMatch;
                    }
                }

                paseadorDto.DistanceKm = Math.Round(distancia, 1);
                encontrados.Add(paseadorDto);
            }

            return encontrados.OrderBy(x => x.DistanceKm).ToList();
        }

        //Horarios libres del paseador que sirven para la busqueda: los de la fecha (y el horario, si se informo) o, sin fecha, los de ese horario
        //en cada uno de los proximos DiasDeBusquedaPorHorario dias.
        private List<AvailableTimeDto> ObtenerCuposParaBusqueda(Int64 idPaseador, DateTime? fecha, String horario)
        {
            var conHorario = !String.IsNullOrWhiteSpace(horario);

            if (fecha.HasValue)
            {
                var delDia = ObtenerCupos(idPaseador, fecha.Value);
                return conHorario ? delDia.Where(x => x.Time == horario).ToList() : delDia;
            }

            var cupos = new List<AvailableTimeDto>();
            for (var i = 0; i < DiasDeBusquedaPorHorario; i++)
            {
                cupos.AddRange(ObtenerCupos(idPaseador, DateTime.Now.Date.AddDays(i)).Where(x => x.Time == horario));
            }
            return cupos;
        }

        private List<Pet> ObtenerMascotas(List<Int64> idsMascotas)
        {
            if (idsMascotas == null || idsMascotas.Count == 0)
            {
                return new List<Pet>();
            }

            return this.repositorioMascotas.BuscarPor(x => idsMascotas.Contains(x.Id)).ToList();
        }

        //Matching entre mascotas: en cada horario busca, entre los perros de otros clientes que el paseador ya lleva (reservas pendientes o confirmadas),
        //los que comparten al menos PetTraits.MinimoEnComun caracteristicas con alguna de las mascotas del cliente.
        //Informa cuantos son y cual es el que mas se parece (con cual mascota del cliente y en que caracteristicas), sin decir de quien es.
        private void AplicarCompatibilidad(Int64 idPaseador, DateTime fecha, List<AvailableTimeDto> cupos, List<Pet> mascotasDelCliente)
        {
            var propias = mascotasDelCliente.Select(x => new { Mascota = x, Rasgos = LeerRasgos(x.Traits) }).
                                             Where(x => x.Rasgos.Count >= PetTraits.MinimoEnComun).
                                             ToList();
            if (propias.Count == 0 || cupos.Count == 0)
            {
                return;
            }

            var idsClientes = mascotasDelCliente.Select(x => x.CustomerId).Distinct().ToList();
            var dia = fecha.Date;
            var perrosDelDia = this.repositorioPaseos.TodosConIncluidos(x => x.Pet, x => x.Pet.Breed, x => x.Pet.Size).
                                                    Where(x => x.WalkerId == idPaseador && x.Date == dia && x.Status != WalkStatus.Cancelled).
                                                    ToList().
                                                    Where(x => !idsClientes.Contains(x.Pet.CustomerId)).
                                                    ToList();

            foreach (var cupo in cupos)
            {
                var coincidencias = 0;
                PetMatchDto mejor = null;

                foreach (var perro in perrosDelDia.Where(x => x.TimeFrom == cupo.Time).Select(x => x.Pet).GroupBy(x => x.Id).Select(x => x.First()))
                {
                    var rasgos = LeerRasgos(perro.Traits);

                    //Con cual de las mascotas del cliente se parece mas
                    var masParecida = propias.Select(x => new { x.Mascota, EnComun = PetTraits.EnComun(x.Rasgos, rasgos) }).
                                              OrderByDescending(x => x.EnComun.Count).
                                              First();
                    if (masParecida.EnComun.Count < PetTraits.MinimoEnComun)
                    {
                        continue;
                    }

                    coincidencias++;
                    if (mejor == null || masParecida.EnComun.Count > mejor.SharedCount)
                    {
                        mejor = new PetMatchDto
                        {
                            YourPetId = masParecida.Mascota.Id,
                            YourPetName = masParecida.Mascota.Name,
                            PetName = perro.Name,
                            BreedName = perro.Breed != null ? perro.Breed.Name : null,
                            SizeName = perro.Size != null ? perro.Size.Name : null,
                            SharedCount = masParecida.EnComun.Count,
                            SharedTraits = CodigosActivos().Where(x => masParecida.EnComun.Contains(x)).ToList()
                        };
                    }
                }

                cupo.MatchingPets = coincidencias;
                cupo.MatchScore = mejor != null ? mejor.SharedCount : 0;
                cupo.SharedTraits = mejor != null ? mejor.SharedTraits : new List<String>();
                cupo.BestMatch = mejor;
            }
        }


        //Horarios libres del paseador en la fecha, con los perros parecidos que lleva en cada uno (para sugerir los horarios al reservar)
        public List<AvailableTimeDto> ObtenerCuposConCompatibilidad(Int64 idPaseador, DateTime fecha, List<Int64> idsMascotas)
        {
            var cupos = ObtenerCupos(idPaseador, fecha);
            var mascotas = ObtenerMascotas(idsMascotas);
            if (mascotas.Count > 0)
            {
                AplicarCompatibilidad(idPaseador, fecha, cupos, mascotas);
            }

            return cupos;
        }

        //Verifica la zona de trabajo y los datos de cobro. Devuelve el mensaje de error, o null si esta todo bien.
        //Al registrarse (esRegistro) la direccion de referencia con sus coordenadas y el radio son obligatorios;
        //al actualizar, un radio en 0 significa "sin cambios".
        public static String ValidarCondiciones(WalkerDto paseadorDto, Boolean esRegistro)
        {
            Double latitud, longitud;
            if (esRegistro && !Geografia.IntentarLeerCoordenadas(paseadorDto.Latitude, paseadorDto.Longitude, out latitud, out longitud))
            {
                return "Elegí la dirección de referencia de la lista de sugerencias o marcala en el mapa.";
            }

            var radio = paseadorDto.ServiceRadiusKm;
            if ((esRegistro || radio != 0) && (radio < RadioMinimoKm || radio > RadioMaximoKm))
            {
                return "El radio de trabajo tiene que estar entre " + RadioMinimoKm + " y " + RadioMaximoKm + " km.";
            }

            var cupo = paseadorDto.MaxPetsAtOnce;
            if ((esRegistro || cupo != 0) && (cupo < MascotasMinimas || cupo > MascotasMaximas))
            {
                return "La cantidad de perros a la vez tiene que estar entre " + MascotasMinimas + " y " + MascotasMaximas + ".";
            }

            if (!String.IsNullOrWhiteSpace(paseadorDto.PayoutAccount))
            {
                var cuenta = paseadorDto.PayoutAccount.Trim();
                var esCbuOCvu = cuenta.Length == 22 && cuenta.All(Char.IsDigit);
                var esAlias = cuenta.Length >= 6 && cuenta.Length <= 20 && cuenta.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || Char.IsDigit(c) || c == '.' || c == '-');
                if (!esCbuOCvu && !esAlias)
                {
                    return "El alias debe tener entre 6 y 20 caracteres (letras, números, puntos o guiones), o ser un CBU/CVU de 22 dígitos.";
                }
            }

            return null;
        }

        public WalkerDto ObtenerDetalle(Int64 id)
        {
            var paseador = this.repositorioPaseadores.TodosConIncluidos(x => x.City, x => x.Price, x => x.City.Province).
                                                Where(x => x.Id == id).
                                                FirstOrDefault();
            if (paseador == null)
            {
                return null;
            }

            var paseadorDto = Mapper.Map<Walker, WalkerDto>(paseador);
            AdjuntarValoraciones(new List<WalkerDto> { paseadorDto });
            paseadorDto.PayoutAccount = null;
            return paseadorDto;
        }

        //Horarios en los que el paseador puede recibir un paseo en la fecha indicada.
        public List<String> ObtenerHorariosDisponibles(Int64 idPaseador, DateTime fecha)
        {
            return ObtenerCupos(idPaseador, fecha).Select(x => x.Time).ToList();
        }

        //Horarios de la fecha en los que al paseador le queda algun lugar libre, con cuantos lugares le quedan.
        //Un paseo dura una hora, por eso el horario de fin de la jornada no se ofrece como inicio.
        //Cada paseador define cuantas mascotas lleva a la vez (MaxPetsAtOnce); un horario sin lugares libres no se ofrece.
        public List<AvailableTimeDto> ObtenerCupos(Int64 idPaseador, DateTime fecha)
        {
            var cupos = new List<AvailableTimeDto>();
            var dia = fecha.Date;
            var ahora = DateTime.Now;

            if (dia < ahora.Date)
            {
                return cupos;
            }

            var paseador = this.repositorioPaseadores.ObtenerUno(idPaseador);
            if (paseador == null)
            {
                return cupos;
            }

            var diaDeLaSemana = new ServicioDiaDeLaSemana().ObtenerDiaDeLaSemana(dia.DayOfWeek.ToString());
            var jornadas = this.repositorioJornadas.ObtenerTodos().
                                                   Where(x => x.WalkerId == idPaseador && x.DayOfWeek == diaDeLaSemana).
                                                   ToList();
            if (jornadas.Count == 0)
            {
                return cupos;
            }

            var paseos = this.repositorioPaseos.ObtenerTodos().
                                             Where(x => x.WalkerId == idPaseador && x.Date == dia && x.Status != WalkStatus.Cancelled).
                                             ToList();

            foreach (var jornada in jornadas)
            {
                var desde = Convert.ToInt32(jornada.TimeFrom.Split(':')[0]);
                var hasta = Convert.ToInt32(jornada.TimeUntil.Split(':')[0]);

                for (var hora = desde; hora < hasta; hora++)
                {
                    //Si es hoy, solo se ofrecen horarios posteriores a la hora actual
                    if (dia == ahora.Date && hora <= ahora.Hour)
                    {
                        continue;
                    }

                    var horario = hora.ToString("00") + ":00";
                    var lugaresLibres = paseador.MaxPetsAtOnce - paseos.Count(x => x.TimeFrom == horario);
                    if (lugaresLibres < 1 || cupos.Any(x => x.Time == horario))
                    {
                        continue;
                    }

                    cupos.Add(new AvailableTimeDto { Date = dia.ToString("yyyy-MM-dd"), Time = horario, FreeSpots = lugaresLibres });
                }
            }

            return cupos.OrderBy(x => x.Time).ToList();
        }
    }
}
