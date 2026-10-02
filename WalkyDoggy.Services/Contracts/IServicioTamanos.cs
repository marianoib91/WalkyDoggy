using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Services.Contracts
{
    public interface IServicioTamanos : IServicioEntidadBase<Size, SizeDto>
    {
        List<SizeDto> ObtenerTodos();
    }
}
