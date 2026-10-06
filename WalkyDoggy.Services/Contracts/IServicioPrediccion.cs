using System;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    //Sugerencias de reserva a partir del historial de cada cliente. Quien llama comprueba que el actor sea ese cliente.
    public interface IServicioPrediccion
    {
        //Null si no hay un patron claro (pocas reservas o dias muy variados) o si ya tiene reservado ese dia
        SugerenciaReservaDto SugerirReserva(Int64 idCliente, DateTime hoy);

        //Los dias mas pedidos del paseador en las ultimas semanas. Null si todavia tiene muy pocas reservas.
        DemandaPaseadorDto DemandaDelPaseador(Int64 idPaseador, DateTime hoy);
    }
}
