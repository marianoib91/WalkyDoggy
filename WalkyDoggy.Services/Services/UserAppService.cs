using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Abstract;
using WalkyDoggy.Services.Dtos;

namespace WalkyDoggy.Services.Contracts
{
    public class UserAppService : EntityBaseAppService<User, UserDto>, IUserAppService
    {
        #region Variables

        private readonly IEncryptionService encryptionService;
        private readonly IMembershipService membershipService;
        #endregion

        public UserAppService(IEntityBaseRepository<Error> errorsRepository,
                              IUnitOfWork unitOfWork,
                              IEntityBaseRepository<User> usersRepository,
                              IEncryptionService encryptionService,
                              IMembershipService membershipService) :
            base(errorsRepository, unitOfWork, usersRepository)
        {

            this.encryptionService = encryptionService;
            this.membershipService = membershipService;
        }

        public UserDto GetByEmail(String email)
        {
            var userDto = this.GetAll().Where(x => x.Email == email).FirstOrDefault();
            return userDto;
        }

        public Int64 GetRole(Boolean isWalker)
        {
            if (isWalker)
            {
                return Roles.Walker;
            }
            return Roles.Customer;
        }
    }
}
