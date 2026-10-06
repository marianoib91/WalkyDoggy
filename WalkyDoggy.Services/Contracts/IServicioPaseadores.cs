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

        List<WalkerDto> ObtenerTodos();

        //Paseadores que trabajan en la direccion de retiro (dentro de su radio), del mas cercano al mas lejano.
        //Con fecha, solo los que tienen algun horario libre ese dia; con fecha y horario, solo los libres a esa hora.
        //Con las mascotas del cliente (idsMascotas, opcional), cada horario trae cuantos perros de caracteristicas parecidas lleva el paseador.
        List<WalkerDto> BuscarParaRetiro(Double latitud, Double longitud, DateTime? fecha, String horario, List<Int64> idsMascotas);

        //Horarios libres del paseador en la fecha, con los perros parecidos a las mascotas del cliente que lleva en cada uno
        List<AvailableTimeDto> ObtenerCuposConCompatibilidad(Int64 idPaseador, DateTime fecha, List<Int64> idsMascotas);

        WalkerDto ObtenerDetalle(Int64 id);

        List<String> ObtenerHorariosDisponibles(Int64 idPaseador, DateTime fecha);
    }
}
