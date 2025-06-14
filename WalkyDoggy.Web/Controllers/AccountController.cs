using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services;
using WalkyDoggy.Services.Utilities;
using WalkyDoggy.Web.Infrastructure.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Services.Abstract;
using WalkyDoggy.Services.Dtos;
using AutoMapper;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/account")]
    public class AccountController : ApiControllerBase
    {
        private readonly IEntityBaseRepository<User> usersRepository;
        private readonly IMembershipService membershipService;
        private readonly IUserAppService userAppService;

        public AccountController(IEntityBaseRepository<User> usersRepository,
                                 IMembershipService membershipService,
                                 IUserAppService userAppService,
                                 IEntityBaseRepository<Error> errorsRepository,
                                 IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.usersRepository = usersRepository;
            this.membershipService = membershipService;
            this.userAppService = userAppService;
        }

        [AllowAnonymous]
        [Route("authenticate")]
        [HttpPost]
        public HttpResponseMessage Login(HttpRequestMessage request, LoginDto user)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                if (ModelState.IsValid)
                {
                    MembershipContext userContext = membershipService.ValidateUser(user.Email, user.Password);

                    if (userContext.User != null)
                    {
                        response = request.CreateResponse(HttpStatusCode.OK, new
                        {
                            id = userContext.User.Id,
                            //En caso de tener mas de un roleId tenemos que implementar dos login, uno para los paseadores y otro para el cliente
                            roleId = userContext.User.UserRoles.Select(x => x.RoleId).FirstOrDefault(),
                            email = userContext.User.Email,
                            success = true
                        });
                    }
                    else
                    {
                        response = request.CreateResponse(HttpStatusCode.OK, new { success = false });
                    }
                }
                else
                    response = request.CreateResponse(HttpStatusCode.OK, new { success = false });

                return response;
            });
        }

        [Route("register")]
        [HttpPost]
        public HttpResponseMessage Register(HttpRequestMessage request, UserDto userDto)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                if (!ModelState.IsValid)
                {
                    response = request.CreateResponse(HttpStatusCode.BadRequest, new { success = false });
                }
                else
                {
                    var newUser = membershipService.CreateUser(userDto);

                    if (newUser != null)
                    {
                        response = request.CreateResponse(HttpStatusCode.OK, new { success = true });
                    }
                    else
                    {
                        response = request.CreateResponse(HttpStatusCode.OK, new { success = false });
                    }
                }

                return response;
            });
        }
    }
}
