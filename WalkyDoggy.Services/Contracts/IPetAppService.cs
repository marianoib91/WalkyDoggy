using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Services.Contracts
{
    public interface IPetAppService : IEntityBaseAppService<Pet, PetDto>
    {
        Pet Register(PetDto petDto);

        Pet Update(PetDto petDto);

        PetDto GetById(Int64 id);

        List<PetDto> GetAllByCustomerId(Int64 customerId);
    }
}
