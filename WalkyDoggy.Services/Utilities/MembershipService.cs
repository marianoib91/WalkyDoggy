using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Entities.Contracts;
using WalkyDoggy.Services.Dtos;
using WalkyDoggy.Services.Utilities;

namespace WalkyDoggy.Services
{
    public class MembershipService : IMembershipService
    {
        #region Variables
        private readonly IEntityBaseRepository<User> userRepository;
        private readonly IEncryptionService encryptionService;
        private readonly IUnitOfWork unitOfWork;
        #endregion
        public MembershipService(IEntityBaseRepository<User> userRepository,
                                 IEncryptionService encryptionService,
                                 IUnitOfWork unitOfWork)
        {
            this.userRepository = userRepository;
            this.encryptionService = encryptionService;
            this.unitOfWork = unitOfWork;
        }

        #region IMembershipService Implementation

        public MembershipContext ValidateUser(String email, String password)
        {
            var membershipCtx = new MembershipContext();
            //var user = userRepository.GetAll().Where(x => x.Email == email).FirstOrDefault();
            var user = userRepository.AllIncluding(x => x.UserRoles).Where(x => x.Email == email).FirstOrDefault();
            if (user != null && isUserValid(user, password))
            {
                membershipCtx.User = user;
                var identity = new GenericIdentity(email);
                membershipCtx.Principal = new GenericPrincipal(identity, new String[] { "Admin" });
            }

            return membershipCtx;
        }
        public User CreateUser(UserDto userDto)
        {
            /* var existingUser = userRepository.GetAll().Where(x => x.Email == userDto.Email).FirstOrDefault();

             if (existingUser != null)
             {
                 throw new Exception("El email ingresado ya se encuentra en uso.");
             }*/

            var passwordSalt = encryptionService.CreateSalt();

            var newUser = Mapper.Map<UserDto, User>(userDto);
            newUser.Salt = passwordSalt;
            newUser.IsLocked = false;
            newUser.HashedPassword = encryptionService.EncryptPassword(userDto.Password, passwordSalt);
            newUser.CreatedDate = DateTime.Now;

            userRepository.Add(newUser);

            unitOfWork.Commit();

            return newUser;
        }

        public User GetUser(Int64 userId)
        {
            return userRepository.GetSingle(userId);
        }

        public Boolean UserExists(String email)
        {
            var existingUser = userRepository.GetAll().
                                            Where(x => x.Email == email).
                                            FirstOrDefault();
            if (existingUser != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Helper methods

        private bool isPasswordValid(User user, String password)
        {
            return String.Equals(encryptionService.EncryptPassword(password, user.Salt), user.HashedPassword);
        }

        private bool isUserValid(User user, String password)
        {
            if (isPasswordValid(user, password))
            {
                return !user.IsLocked;
            }

            return false;
        }
        #endregion
    }
}
