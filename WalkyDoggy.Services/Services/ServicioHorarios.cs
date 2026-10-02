using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ServicioHorarios : IServicioHorarios
    {

        public ServicioHorarios()
        {
        }
        public List<TimeDto> ObtenerHorariosDePaseoDisponibles()
        {
            var horarioActual = DateTime.Now.TimeOfDay;
            var horariosDto = new List<TimeDto>();
            return horariosDto;
        }

        public List<TimeDto> ObtenerTodosLosHorariosDePaseo()
        {
            var horarioActual = DateTime.Now.TimeOfDay;
            var horariosDto = new List<TimeDto>();
            return horariosDto;
        }
    }
}
