using System;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    //Metricas del sistema para el administrador (reservas, cancelaciones, cobros, valoraciones, denuncias).
    //Quien llama es responsable de comprobar que el usuario sea administrador (ver IdentidadDelActor).
    public interface IServicioTablero
    {
        //El periodo es por dia del paseo (inclusive). Sin fechas: los ultimos 30 dias. Maximo un año.
        //comisionPorcentaje: la comision de Mercado Pago configurada (0 = no se cobra), para estimar lo que le corresponde a WalkyDoggy.
        DashboardDto Obtener(DateTime? desde, DateTime? hasta, Decimal comisionPorcentaje);
    }
}
