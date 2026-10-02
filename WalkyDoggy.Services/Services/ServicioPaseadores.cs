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
            Mapper.Map(paseadorDto, paseador);

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
            return paseadoresDto;
        }

        public List<WalkerDto> ObtenerTodosOrdenadosPorDistancia(Int64 idCliente)
        {
            var paseadoresDto = ObtenerTodos();

            var cliente = this.repositorioClientes.ObtenerUno(idCliente);
            double latitudCliente, longitudCliente;
            if (cliente == null || !IntentarLeerCoordenadas(cliente.Latitude, cliente.Longitude, out latitudCliente, out longitudCliente))
            {
                return paseadoresDto;
            }

            foreach (var paseadorDto in paseadoresDto)
            {
                double latitudPaseador, longitudPaseador;
                if (IntentarLeerCoordenadas(paseadorDto.Latitude, paseadorDto.Longitude, out latitudPaseador, out longitudPaseador))
                {
                    paseadorDto.DistanceKm = Math.Round(DistanciaEnKilometros(latitudCliente, longitudCliente, latitudPaseador, longitudPaseador), 1);
                }
            }

            return paseadoresDto.OrderBy(x => x.DistanceKm.HasValue ? 0 : 1).
                              ThenBy(x => x.DistanceKm).
                              ToList();
        }

        private static Boolean IntentarLeerCoordenadas(String latitud, String longitud, out Double latitudLeida, out Double longitudLeida)
        {
            latitudLeida = 0;
            longitudLeida = 0;

            return Double.TryParse(latitud, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out latitudLeida) &&
                   Double.TryParse(longitud, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out longitudLeida);
        }

        //Distancia en linea recta entre dos puntos (formula de Haversine)
        private static Double DistanciaEnKilometros(Double latitud1, Double longitud1, Double latitud2, Double longitud2)
        {
            const Double radioTierraKm = 6371;
            Func<Double, Double> aRadianes = grados => grados * Math.PI / 180;

            var deltaLatitud = aRadianes(latitud2 - latitud1);
            var deltaLongitud = aRadianes(longitud2 - longitud1);
            var a = Math.Sin(deltaLatitud / 2) * Math.Sin(deltaLatitud / 2) +
                    Math.Cos(aRadianes(latitud1)) * Math.Cos(aRadianes(latitud2)) *
                    Math.Sin(deltaLongitud / 2) * Math.Sin(deltaLongitud / 2);

            return radioTierraKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
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

        public List<WalkerDto> ObtenerPaseadoresDisponibles(AvailableWalkersCriteria criterioPaseadoresDisponibles)
        {
            var servicioDiaDeLaSemana = new ServicioDiaDeLaSemana();
            var fecha = DateTime.Parse(criterioPaseadoresDisponibles.Date).Date;
            var diaDeLaSemana = servicioDiaDeLaSemana.ObtenerDiaDeLaSemana(fecha.DayOfWeek.ToString());
            var horaDesde = Convert.ToInt64(criterioPaseadoresDisponibles.TimeFrom.Split(':')[0]);

            //Se obtienen las jornadas laborales del dia de la semana seleccionado
            var jornadas = this.repositorioJornadas.TodosConIncluidos(x => x.Walker.City.Province,
                                                                x => x.Walker.Price).
                                                               Where(x => x.DayOfWeek == diaDeLaSemana).
                                                               ToList();

            //Se obtienen los paseos registrados con ese dia y hora
            var paseos = this.repositorioPaseos.ObtenerTodos().Where(x => x.Date == fecha &&
                                                               x.TimeFrom == criterioPaseadoresDisponibles.TimeFrom).
                                                      ToList();

          
            //Listado de paseadores disponibles
            var paseadoresDisponibles = new List<Walker>();

            foreach (var jornada in jornadas)
            {
                //Se valida que el horario seleccionado este dentro de las jornadas laborales seleccionadas anteriormente
                if (Convert.ToInt64(jornada.TimeFrom.Split(':')[0]) <= horaDesde &&
                    Convert.ToInt64(jornada.TimeUntil.Split(':')[0]) >= horaDesde)
                {
                    //Si hay paseos ese dia y a esa hora se hacen las validaciones correspondientes
                    if (paseos.Count > 0)
                    {
                        var paseosDelPaseador = paseos.Where(x => x.WalkerId == jornada.WalkerId);

                        //Puede pasear hasta 5 mascotas a la vez
                        if (paseosDelPaseador.Count() > 5)
                        {
                            //si tiene mas de 5 para ese dia y hora no se muestra el paseador
                        }
                        else
                        {
                            paseadoresDisponibles.Add(jornada.Walker);
                        }
                    }
                    //Se agrega directamente el paseador disponible en ese dia y hora porque no hay paseos con ese dia y hora
                    else
                    {
                        paseadoresDisponibles.Add(jornada.Walker);
                    }
                }
            }
            var paseadoresDto = Mapper.Map<List<Walker>, List<WalkerDto>>(paseadoresDisponibles);
            return paseadoresDto;
        }
    }
}
