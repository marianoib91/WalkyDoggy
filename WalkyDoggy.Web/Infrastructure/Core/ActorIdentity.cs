using System;
using System.Linq;
using System.Security.Principal;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Web.Infrastructure.Core
{
    //Comprueba que quien llama a la API inicio sesion como el cliente o el paseador sobre el que quiere actuar.
    //Se usa en las acciones que mueven dinero o cambian el estado de un paseo.
    public static class ActorIdentity
    {
        public static Boolean IsAuthenticated(IPrincipal user)
        {
            return user != null && user.Identity != null && user.Identity.IsAuthenticated;
        }

        public static Boolean IsCustomer(IPrincipal user, IEntityBaseRepository<Customer> customersRepository, Int64 customerId)
        {
            if (!IsAuthenticated(user))
            {
                return false;
            }

            var customer = customersRepository.AllIncluding(x => x.User).FirstOrDefault(x => x.Id == customerId);
            return customer != null && customer.User != null &&
                   String.Equals(customer.User.Email, user.Identity.Name, StringComparison.OrdinalIgnoreCase);
        }

        public static Boolean IsWalker(IPrincipal user, IEntityBaseRepository<Walker> walkersRepository, Int64 walkerId)
        {
            if (!IsAuthenticated(user))
            {
                return false;
            }

            var walker = walkersRepository.AllIncluding(x => x.User).FirstOrDefault(x => x.Id == walkerId);
            return walker != null && walker.User != null &&
                   String.Equals(walker.User.Email, user.Identity.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
