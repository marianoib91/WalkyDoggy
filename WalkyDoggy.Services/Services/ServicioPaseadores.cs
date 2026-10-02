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
        //Cantidad maxima de mascotas que un paseador puede llevar a la vez
        public const Int32 MaximoMascotasPorPaseo = 5;

        #region Variables
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;
        private readonly IRepositorioEntidadBase<WorkDay> repositorioJornadas;
        private readonly IRepositorioEntidadBase<Walk> repositorioPaseos;
        private readonly IRepositorioEntidadBase<Ranking> repositorioValoraciones;
        private readonly IRepositorioEntidadBase<Price> repositorioPrecios;
        private readonly IRepositorioEntidadBase<UserRole> repositorioRolesUsuario;
        private readonly IServicioEncriptacion servicioEncriptacion;
        private readonly IServicioMembresia servicioMembresia;
        #endregion

        public ServicioPaseadores(IRepositorioEntidadBase<Error> repositorioErrores,
                                IUnidadDeTrabajo unidadDeTrabajo,
                                IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                IRepositorioEntidadBase<Customer> repositorioClientes,
                                IRepositorioEntidadBase<WorkDay> repositorioJornadas,
                                IRepositorioEntidadBase<Walk> repositorioPaseos,
                                IRepositorioEntidadBase<Ranking> repositorioValoraciones,
                                IRepositorioEntidadBase<Price> repositorioPrecios,
                                IRepositorioEntidadBase<UserRole> repositorioRolesUsuario,
                                IServicioEncriptacion servicioEncriptacion,
                                IServicioMembresia servicioMembresia) :
            base(repositorioErrores, unidadDeTrabajo, repositorioPaseadores)
        {
            this.repositorioPaseadores = repositorioPaseadores;
            this.repositorioClientes = repositorioClientes;
            this.repositorioJornadas = repositorioJornadas;
            this.repositorioPaseos = repositorioPaseos;
            this.repositorioValoraciones = repositorioValoraciones;
            this.repositorioPrecios = repositorioPrecios;
            this.repositorioRolesUsuario = repositorioRolesUsuario;
            this.servicioEncriptacion = servicioEncriptacion;
            this.servicioMembresia = servicioMembresia;
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
            Mapper.Map(paseadorDto, paseador);

            //Sin radio informado queda el que tenia; sin cuenta informada (null) tambien. Un texto vacio borra la cuenta.
            if (paseadorDto.ServiceRadiusKm <= 0)
            {
                paseador.ServiceRadiusKm = radioAnterior;
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
        //Si se informa la fecha, solo los que tienen algun horario libre ese dia; si ademas se informa el horario, solo los libres a esa hora.
        public List<WalkerDto> BuscarParaRetiro(Double latitud, Double longitud, DateTime? fecha, String horario)
        {
            var paseadoresDto = ObtenerTodos();
            var encontrados = new List<WalkerDto>();

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

                if (fecha.HasValue)
                {
                    var horariosLibres = ObtenerHorariosDisponibles(paseadorDto.Id, fecha.Value);
                    if (String.IsNullOrWhiteSpace(horario) ? horariosLibres.Count == 0 : !horariosLibres.Contains(horario))
                    {
                        continue;
                    }
                }

                paseadorDto.DistanceKm = Math.Round(distancia, 1);
                encontrados.Add(paseadorDto);
            }

            return encontrados.OrderBy(x => x.DistanceKm).ToList();
        }

        //Verifica la zona de trabajo y los datos de cobro. Devuelve el mensaje de error, o null si esta todo bien.
        //Al registrarse (exigirUbicacion) la direccion de referencia con sus coordenadas y el radio son obligatorios;
        //al actualizar, un radio en 0 significa "sin cambios".
        public static String ValidarZonaYCobro(WalkerDto paseadorDto, Boolean exigirUbicacion)
        {
            Double latitud, longitud;
            if (exigirUbicacion && !Geografia.IntentarLeerCoordenadas(paseadorDto.Latitude, paseadorDto.Longitude, out latitud, out longitud))
            {
                return "Elegí la dirección de referencia de la lista de sugerencias o marcala en el mapa.";
            }

            var radio = paseadorDto.ServiceRadiusKm;
            if ((exigirUbicacion || radio != 0) && (radio < RadioMinimoKm || radio > RadioMaximoKm))
            {
                return "El radio de trabajo tiene que estar entre " + RadioMinimoKm + " y " + RadioMaximoKm + " km.";
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
        //Un paseo dura una hora, por eso el horario de fin de la jornada no se ofrece como inicio.
        public List<String> ObtenerHorariosDisponibles(Int64 idPaseador, DateTime fecha)
        {
            var horarios = new List<String>();
            var dia = fecha.Date;
            var ahora = DateTime.Now;

            if (dia < ahora.Date)
            {
                return horarios;
            }

            var diaDeLaSemana = new ServicioDiaDeLaSemana().ObtenerDiaDeLaSemana(dia.DayOfWeek.ToString());
            var jornadas = this.repositorioJornadas.ObtenerTodos().
                                                   Where(x => x.WalkerId == idPaseador && x.DayOfWeek == diaDeLaSemana).
                                                   ToList();
            if (jornadas.Count == 0)
            {
                return horarios;
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
                    var mascotasEnPaseo = paseos.Count(x => x.TimeFrom == horario);
                    if (mascotasEnPaseo >= MaximoMascotasPorPaseo)
                    {
                        continue;
                    }

                    if (!horarios.Contains(horario))
                    {
                        horarios.Add(horario);
                    }
                }
            }

            horarios.Sort();
            return horarios;
        }
    }
}
