using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Entities.Entities;

namespace WalkyDoggy.Services.Contracts
{
    public interface IServicioJornadas : IServicioEntidadBase<WorkDay, WorkDayDto>
    {
        List<WorkDayDto> ObtenerTodos();

        List<WorkDayDto> ObtenerTodosPorIdPaseador(Int64 idPaseador);

        void Eliminar(Int64 id);

        WorkDay Guardar(WorkDayDto jornadaDto);

        //Reemplaza todas las franjas horarias de la semana del paseador (cualquier dia, de 00:00 a 24:00)
        Boolean SaveWeek(Int64 idPaseador, List<WorkRangeDto> franjas, out String error);
    }
}
