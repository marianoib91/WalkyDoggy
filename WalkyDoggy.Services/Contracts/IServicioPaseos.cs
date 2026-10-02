using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Criterias;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Services.Contracts
{
    public interface IServicioPaseos : IServicioEntidadBase<Walk, WalkDto>
    {
        List<WalkDto> ObtenerTodos();

        List<WalkDto> ObtenerTodosPorIdPaseador(Int64 idPaseador);

        List<WalkDto> ObtenerTodosDelDiaActual(Int64 idPaseador);

        WalkDto ValidarMascotasEnPaseos(AvailableWalkersCriteria criterioPaseadoresDisponibles);

        //Reservas del paseador desde hoy que esperan su respuesta o ya estan confirmadas
        List<BookingDto> ObtenerReservasDelPaseador(Int64 idPaseador);

        //Todas las reservas del cliente, de la mas reciente a la mas antigua
        List<BookingDto> ObtenerReservasDelCliente(Int64 idCliente);

        //El paseador confirma una reserva que esta esperando su respuesta
        Boolean Confirmar(BookingActionCriteria criterioAccionReserva, out String error);

        //El paseador o el cliente cancelan una reserva que todavia no empezo
        Boolean Cancelar(BookingActionCriteria criterioAccionReserva, out String error);

        //El paseador da por finalizado un paseo que ya empezo (devolvio a la mascota): desde ahi el cliente puede pagarlo
        Boolean Finalizar(BookingActionCriteria criterioAccionReserva, out String error);

        //El paseador confirma que recibio el pago (en efectivo o con Mercado Pago) y la reserva queda cerrada
        Boolean ConfirmarRecibido(BookingActionCriteria criterioAccionReserva, out String error);

        //Crea un paseo por cada mascota. Si no se puede reservar devuelve null y el motivo en error.
        List<WalkDto> Registrar(WalkRequestCriteria criterioSolicitudPaseo, out String error);
    }
}
