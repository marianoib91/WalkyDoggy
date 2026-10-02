using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Services.Contracts
{
    public interface IServicioClientes : IServicioEntidadBase<Customer, CustomerDto>
    {
        CustomerDto Registrar(CustomerDto clienteDto);

        CustomerDto ObtenerPorIdUsuario(Int64 idUsuario);

        void Actualizar(CustomerDto clienteDto);
    }
}
