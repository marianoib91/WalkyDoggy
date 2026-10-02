using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Entities.Entities;
using WalkyDoggy.Services.Contracts;

namespace WalkyDoggy.Services.Services
{
    public class ServicioJornadas : ServicioEntidadBase<WorkDay, WorkDayDto>, IServicioJornadas
    {
        #region Variables
        private readonly IRepositorioEntidadBase<WorkDay> repositorioJornadas;
        #endregion

        public ServicioJornadas(IRepositorioEntidadBase<Error> repositorioErrores,
                                  IUnidadDeTrabajo unidadDeTrabajo,
                                  IRepositorioEntidadBase<WorkDay> repositorioJornadas) :
            base(repositorioErrores, unidadDeTrabajo, repositorioJornadas)
        {
            this.repositorioJornadas = repositorioJornadas;
        }

        public List<WorkDayDto> ObtenerTodos()
        {
            var jornadas = this.repositorioEntidad.ObtenerTodos().ToList();
            var jornadasDto = Mapper.Map<List<WorkDay>, List<WorkDayDto>>(jornadas);
            return jornadasDto;
        }


        public List<WorkDayDto> ObtenerTodosPorIdPaseador(Int64 idPaseador)
        {
            var jornadas = this.repositorioEntidad.ObtenerTodos().Where(x => x.WalkerId == idPaseador).ToList();
            var jornadasDto = Mapper.Map<List<WorkDay>, List<WorkDayDto>>(jornadas);
            return jornadasDto;
        }

        public WorkDay Guardar(WorkDayDto jornadaDto)
        {
            if (jornadaDto.Id == 0)
            {
                var jornadaExistente = this.repositorioEntidad.ObtenerTodos().Where(x => x.WalkerId == jornadaDto.WalkerId &&
                                                                              x.DayOfWeek == jornadaDto.DayOfWeek &&
                                                                              x.TimeFrom == jornadaDto.TimeFrom &&
                                                                              x.TimeUntil == jornadaDto.TimeUntil).FirstOrDefault();
                if (jornadaExistente == null)
                {
                    var jornada = Mapper.Map<WorkDayDto, WorkDay>(jornadaDto);
                    this.repositorioEntidad.Agregar(jornada);
                    this.unidadDeTrabajo.GuardarCambios();
                    return jornada;
                }
                else
                {
                    return null;
                }
             
            }
            else
            {
                var jornada = Mapper.Map<WorkDayDto, WorkDay>(jornadaDto);
                this.repositorioEntidad.Editar(jornada);
                this.unidadDeTrabajo.GuardarCambios();
                return jornada;
            }

        }

        private static readonly String[] Dias = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };

        //Reemplaza todas las franjas horarias del paseador por las indicadas. Se puede trabajar cualquier dia a cualquier hora
        //(de 00:00 a 24:00). Las franjas que se pisan o quedan pegadas se unen en una sola.
        public Boolean SaveWeek(Int64 idPaseador, List<WorkRangeDto> franjas, out String error)
        {
            error = null;

            var trabaja = new Dictionary<String, Boolean[]>();
            foreach (var dia in Dias)
            {
                trabaja[dia] = new Boolean[24];
            }

            foreach (var franja in franjas ?? new List<WorkRangeDto>())
            {
                var dia = Dias.FirstOrDefault(x => x == franja.DayOfWeek);
                if (dia == null)
                {
                    error = "El día \"" + franja.DayOfWeek + "\" no es válido.";
                    return false;
                }

                Int32 desde, hasta;
                if (!IntentarLeerHora(franja.TimeFrom, 0, 23, out desde) || !IntentarLeerHora(franja.TimeUntil, 1, 24, out hasta) || hasta <= desde)
                {
                    error = "El horario " + franja.TimeFrom + " a " + franja.TimeUntil + " del " + dia + " no es válido: usá horas en punto y que termine después de empezar.";
                    return false;
                }

                for (var hora = desde; hora < hasta; hora++)
                {
                    trabaja[dia][hora] = true;
                }
            }

            foreach (var existente in this.repositorioJornadas.ObtenerTodos().Where(x => x.WalkerId == idPaseador).ToList())
            {
                this.repositorioJornadas.Eliminar(existente);
            }

            foreach (var dia in Dias)
            {
                var hora = 0;
                while (hora < 24)
                {
                    if (!trabaja[dia][hora])
                    {
                        hora++;
                        continue;
                    }

                    var inicio = hora;
                    while (hora < 24 && trabaja[dia][hora])
                    {
                        hora++;
                    }

                    this.repositorioJornadas.Agregar(new WorkDay
                    {
                        WalkerId = idPaseador,
                        DayOfWeek = dia,
                        TimeFrom = inicio.ToString("00") + ":00",
                        TimeUntil = hora.ToString("00") + ":00"
                    });
                }
            }

            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        private static Boolean IntentarLeerHora(String texto, Int32 min, Int32 max, out Int32 hora)
        {
            hora = 0;
            var coincidencia = System.Text.RegularExpressions.Regex.Match(texto ?? String.Empty, @"^(\d{1,2}):00$");
            return coincidencia.Success && Int32.TryParse(coincidencia.Groups[1].Value, out hora) && hora >= min && hora <= max;
        }

        public void Eliminar(Int64 id)
        {
            var jornada = this.repositorioEntidad.ObtenerUno(id);
            this.repositorioEntidad.Eliminar(jornada);
            this.unidadDeTrabajo.GuardarCambios();

        }
    }
}
