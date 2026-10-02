using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Services.Contracts
{
    public interface IServicioMascotas : IServicioEntidadBase<Pet, PetDto>
    {
        Pet Registrar(PetDto mascotaDto);

        Pet Actualizar(PetDto mascotaDto);

        PetDto ObtenerPorId(Int64 id);

        List<PetDto> ObtenerTodosPorIdCliente(Int64 idCliente);
    }
}
