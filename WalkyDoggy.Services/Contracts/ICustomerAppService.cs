using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Services.Contracts
{
    public interface ICustomerAppService : IEntityBaseAppService<Customer, CustomerDto>
    {
        CustomerDto Register(CustomerDto customerDto);

        CustomerDto GetByUserId(Int64 userId);

        void Update(CustomerDto customerDto);
    }
}
