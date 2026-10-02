using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Services.Dtos;

namespace WalkyDoggy.Services.Services
{
    public class ServicioClientes : ServicioEntidadBase<Customer, CustomerDto>, IServicioClientes
    {
        #region Variables
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;
        private readonly IRepositorioEntidadBase<User> repositorioUsuarios;
        private readonly IRepositorioEntidadBase<UserRole> repositorioRolesUsuario;
        private readonly IServicioEncriptacion servicioEncriptacion;
        private readonly IServicioMembresia servicioMembresia;
        #endregion

        public ServicioClientes(IRepositorioEntidadBase<Error> repositorioErrores,
                                  IUnidadDeTrabajo unidadDeTrabajo,
                                  IRepositorioEntidadBase<Customer> repositorioClientes,
                                  IRepositorioEntidadBase<User> repositorioUsuarios,
                                  IRepositorioEntidadBase<UserRole> repositorioRolesUsuario,
                                  IServicioEncriptacion servicioEncriptacion,
                                  IServicioMembresia servicioMembresia) :
            base(repositorioErrores, unidadDeTrabajo, repositorioClientes)
        {
            this.repositorioClientes = repositorioClientes;
            this.repositorioUsuarios = repositorioUsuarios;
            this.repositorioRolesUsuario = repositorioRolesUsuario;
            this.servicioEncriptacion = servicioEncriptacion;
            this.servicioMembresia = servicioMembresia;
        }

        public CustomerDto Registrar(CustomerDto clienteDto)
        {
            //Se registra el usuario correspondiente al cliente
            var usuarioDto = new UserDto();
            usuarioDto.Email = clienteDto.Email;
            usuarioDto.CreatedDate = DateTime.Now;
            usuarioDto.IsLocked = false;
            usuarioDto.Password = clienteDto.Password;
            var usuarioCreado = this.servicioMembresia.CrearUsuario(usuarioDto);

            //Se registra el rol del usuario en la tabla UserRoles
            var rolUsuario = new UserRole();
            rolUsuario.UserId = usuarioCreado.Id;
            rolUsuario.RoleId = Roles.Customer;
            this.repositorioRolesUsuario.Agregar(rolUsuario);

            //Se registra el cliente
            var cliente = Mapper.Map<CustomerDto, Customer>(clienteDto);
            cliente.UserId = usuarioCreado.Id;
            this.repositorioClientes.Agregar(cliente);

            //Se guardan los registros en la base de datos y se devuelve el cliente nuevo
            this.unidadDeTrabajo.GuardarCambios();

            //Se devuelve el roleId para usarlo en la presentacion de las vistas de acuerdo al rol del usuario
            clienteDto.RoleId = rolUsuario.RoleId;
            clienteDto.UserId = cliente.UserId;

            return clienteDto;
        }

        public CustomerDto ObtenerPorIdUsuario(Int64 idUsuario)
        {
            var cliente = this.repositorioClientes.TodosConIncluidos(x => x.City, x => x.City.Province).Where(x => x.UserId == idUsuario).FirstOrDefault();

            var clienteDto = Mapper.Map<Customer, CustomerDto>(cliente);

            return clienteDto;
        }

        public void Actualizar(CustomerDto clienteDto)
        {
            var cliente = Mapper.Map<CustomerDto, Customer>(clienteDto);
            this.repositorioClientes.Editar(cliente);
            this.unidadDeTrabajo.GuardarCambios();
        }
    }
}
