using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/customers")]
    public class CustomersController : ApiControllerBase
    {
        private readonly IEntityBaseRepository<Customer> customersRepository;
        private readonly IMembershipService membershipService;
        private readonly ICustomerAppService customerAppService;

        public CustomersController(IEntityBaseRepository<Customer> customersRepository,
                                 IMembershipService membershipService,
                                 ICustomerAppService customerAppService,
                                 IEntityBaseRepository<Error> errorsRepository,
                                 IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.customersRepository = customersRepository;
            this.membershipService = membershipService;
            this.customerAppService = customerAppService;
        }

        [HttpPost]
        [Route("register")]
        public HttpResponseMessage Register(HttpRequestMessage request, CustomerDto customerDto)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                if (!ModelState.IsValid)
                {
                    response = request.CreateResponse(HttpStatusCode.BadRequest,
                        ModelState.Keys.SelectMany(k => ModelState[k].Errors)
                              .Select(m => m.ErrorMessage).ToArray());
                }
                else
                {
                    if (membershipService.UserExists(customerDto.Email))
                    {
                        ModelState.AddModelError("E-mail invalido", "El email ingresado ya se encuentra en uso.");
                        response = request.CreateResponse(HttpStatusCode.BadRequest,
                        ModelState.Keys.SelectMany(k => ModelState[k].Errors)
                              .Select(m => m.ErrorMessage).ToArray());
                    }
                    else
                    {
                        var customer = this.customerAppService.Register(customerDto);
                        response = request.CreateResponse<CustomerDto>(HttpStatusCode.OK, customer);
                    }
                }

                return response;
            });
        }

        [HttpGet]
        [Route("getByUserId")]
        public HttpResponseMessage GetByUserId(HttpRequestMessage request, Int64 userId)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;
                var customerDto = this.customerAppService.GetByUserId(userId);

                response = request.CreateResponse(HttpStatusCode.OK, customerDto);

                return response;
            });
        }

        [HttpPost]
        [Route("update")]
        public HttpResponseMessage Update(HttpRequestMessage request, CustomerDto customerDto)
        {
            return CreateHttpResponse(request, () =>
            {

                HttpResponseMessage response = null;
                this.customerAppService.Update(customerDto);

                response = request.CreateResponse(HttpStatusCode.OK, customerDto);

                return response;
            });
        }
    }
}