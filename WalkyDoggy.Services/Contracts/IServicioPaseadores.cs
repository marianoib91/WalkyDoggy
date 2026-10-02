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
    public interface IServicioPaseadores : IServicioEntidadBase<Walker, WalkerDto>
    {
        WalkerDto Registrar(WalkerDto paseadorDto);

        WalkerDto ObtenerPorIdUsuario(Int64 idUsuario);

        void Actualizar(WalkerDto paseadorDto);

        List<WalkerDto> ObtenerPaseadoresDisponibles(AvailableWalkersCriteria criterioPaseadoresDisponibles);

        List<WalkerDto> ObtenerTodos();

        //Todos los paseadores ordenados por cercania al domicilio del cliente (los que no tienen coordenadas van al final)
        List<WalkerDto> ObtenerTodosOrdenadosPorDistancia(Int64 idCliente);

        WalkerDto ObtenerDetalle(Int64 id);

        List<String> ObtenerHorariosDisponibles(Int64 idPaseador, DateTime fecha);
    }
}
