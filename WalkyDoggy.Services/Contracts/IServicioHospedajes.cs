using System;
using System.Collections.Generic;
using WalkyDoggy.Application.Criterias;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    //Hospedaje: perros que pasan varias noches en la casa de un cuidador (un paseador que activa la opcion).
    //Quien llama es responsable de comprobar que el actor sea ese paseador o cliente (ver IdentidadDelActor).
    public interface IServicioHospedajes
    {
        BoardingOfferDto ObtenerOferta(Int64 idPaseador);

        Boolean GuardarOferta(BoardingOfferDto oferta, out String error);

        //Cuidadores con lugar en todas las noches [entrada, salida) para esa cantidad de perros. Con las coordenadas del cliente se ordenan por cercania.
        List<BoardingCarerDto> Buscar(String entrada, String salida, Int32 perros, String latitud, String longitud, out String error);

        StayDto Solicitar(StayRequestDto solicitud, out String error);

        List<StayDto> ObtenerDelPaseador(Int64 idPaseador);

        List<StayDto> ObtenerDelCliente(Int64 idCliente);

        //Las acciones usan BookingKey = "h" + id del hospedaje
        Boolean Confirmar(BookingActionCriteria accion, out String error);

        //Hasta que el cuidador recibe a los perros, cualquiera de las dos partes puede cancelar
        Boolean Cancelar(BookingActionCriteria accion, out String error);

        //El cuidador recibio a los perros
        Boolean Iniciar(BookingActionCriteria accion, out String error);

        //El cuidador devolvio a los perros
        Boolean Finalizar(BookingActionCriteria accion, out String error);

        //El cuidador confirma que cobro
        Boolean ConfirmarRecibido(BookingActionCriteria accion, out String error);
    }
}
