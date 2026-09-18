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
    public interface IWalkAppService : IEntityBaseAppService<Walk, WalkDto>
    {
        List<WalkDto> GetAll();

        List<WalkDto> GetAllByWalkerId(Int64 walkerId);

        List<WalkDto> GetAllForCurrentDay(Int64 walkerId);

        WalkDto ValidatePetsInWalks(AvailableWalkersCriteria availableWalkersCriteria);

        //Reservas del paseador desde hoy que esperan su respuesta o ya estan confirmadas
        List<BookingDto> GetBookingsForWalker(Int64 walkerId);

        //Todas las reservas del cliente, de la mas reciente a la mas antigua
        List<BookingDto> GetBookingsForCustomer(Int64 customerId);

        //El paseador confirma una reserva que esta esperando su respuesta
        Boolean Confirm(BookingActionCriteria bookingActionCriteria, out String error);

        //El paseador o el cliente cancelan una reserva que todavia no empezo
        Boolean Cancel(BookingActionCriteria bookingActionCriteria, out String error);

        //Crea un paseo por cada mascota. Si no se puede reservar devuelve null y el motivo en error.
        List<WalkDto> Register(WalkRequestCriteria walkRequestCriteria, out String error);
    }
}
