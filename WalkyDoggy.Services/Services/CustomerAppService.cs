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
    public class CustomerAppService : EntityBaseAppService<Customer, CustomerDto>, ICustomerAppService
    {
        #region Variables
        private readonly IEntityBaseRepository<Customer> customersRepository;
        private readonly IEntityBaseRepository<User> usersRepository;
        private readonly IEntityBaseRepository<UserRole> userRolesRepository;
        private readonly IEncryptionService encryptionService;
        private readonly IMembershipService membershipService;
        #endregion

        public CustomerAppService(IEntityBaseRepository<Error> errorsRepository,
                                  IUnitOfWork unitOfWork,
                                  IEntityBaseRepository<Customer> customersRepository,
                                  IEntityBaseRepository<User> usersRepository,
                                  IEntityBaseRepository<UserRole> userRolesRepository,
                                  IEncryptionService encryptionService,
                                  IMembershipService membershipService) :
            base(errorsRepository, unitOfWork, customersRepository)
        {
            this.customersRepository = customersRepository;
            this.usersRepository = usersRepository;
            this.userRolesRepository = userRolesRepository;
            this.encryptionService = encryptionService;
            this.membershipService = membershipService;
        }

        public CustomerDto Register(CustomerDto customerDto)
        {
            //Se registra el usuario correspondiente al cliente
            var userDto = new UserDto();
            userDto.Email = customerDto.Email;
            userDto.CreatedDate = DateTime.Now;
            userDto.IsLocked = false;
            userDto.Password = customerDto.Password;
            var createdUser = this.membershipService.CreateUser(userDto);

            //Se registra el rol del usuario en la tabla UserRoles
            var userRole = new UserRole();
            userRole.UserId = createdUser.Id;
            userRole.RoleId = Roles.Customer;
            this.userRolesRepository.Add(userRole);

            //Se registra el cliente
            var customer = Mapper.Map<CustomerDto, Customer>(customerDto);
            customer.UserId = createdUser.Id;
            this.customersRepository.Add(customer);

            //Se guardan los registros en la base de datos y se devuelve el cliente nuevo
            this.unitOfWork.Commit();

            //Se devuelve el roleId para usarlo en la presentacion de las vistas de acuerdo al rol del usuario
            customerDto.RoleId = userRole.RoleId;
            customerDto.UserId = customer.UserId;

            return customerDto;
        }

        public CustomerDto GetByUserId(Int64 userId)
        {
            var customer = this.customersRepository.AllIncluding(x => x.City).Where(x => x.UserId == userId).FirstOrDefault();

            var customerDto = Mapper.Map<Customer, CustomerDto>(customer);

            return customerDto;
        }

        public void Update(CustomerDto customerDto)
        {
            var customer = Mapper.Map<CustomerDto, Customer>(customerDto);
            this.customersRepository.Edit(customer);
            this.unitOfWork.Commit();
        }
    }
}
