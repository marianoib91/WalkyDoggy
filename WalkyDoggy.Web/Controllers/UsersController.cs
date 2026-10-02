using AutoMapper;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Web.Infrastructure.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WalkyDoggy.Web.Infrastructure.Extensions;
using WalkyDoggy.Data.Extensions;
using WalkyDoggy.Services.Dtos;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/users")]
    public class UsersController : ControladorApiBase
    {
        private readonly IRepositorioEntidadBase<User> repositorioUsuarios;

        public UsersController(IRepositorioEntidadBase<User> repositorioUsuarios,
                               IRepositorioEntidadBase<Error> repositorioErrores,
                               IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.repositorioUsuarios = repositorioUsuarios;
        }

        /*[HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage request, string filter)
        {
            filter = filter.ToLower().Trim();

            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                var users = usersRepository.GetAll()
                    .Where(c => c.Email.ToLower().Contains(filter) ||
                                c.FirstName.ToLower().Contains(filter) ||
                                c.LastName.ToLower().Contains(filter)).
                                ToList();

                var usersVm = Mapper.Map<IEnumerable<User>, IEnumerable<UserViewModel>>(users);

                response = request.CreateResponse<IEnumerable<UserViewModel>>(HttpStatusCode.OK, usersVm);

                return response;
            });
        }*/

        /*  [Route("getById/{id}")]
          public HttpResponseMessage GetById(HttpRequestMessage request, [FromUri]Int64 id)
          {
              return CreateHttpResponse(request, () =>
              {
                  HttpResponseMessage response = null;
                  var user = usersRepository.GetSingle(id);

                  UserViewModel userVm = Mapper.Map<User, UserViewModel>(user);

                  response = request.CreateResponse<UserViewModel>(HttpStatusCode.OK, userVm);

                  return response;
              });
          }*/

        [HttpGet]
        [Route("getByEmail")]
        public HttpResponseMessage GetByEmail(HttpRequestMessage pedido, String email)
        {
            email = email.ToLower().Trim();

            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage respuesta = null;

                //TODO: Pasar esto al Service
                //var user = usersRepository.GetAll().Where(x => x.Email == email).FirstOrDefault();
                var usuario = this.repositorioUsuarios.ObtenerUnoPorEmail(email);

                var usuarioDto = Mapper.Map<User, UserDto>(usuario);

                respuesta = pedido.CreateResponse<UserDto>(HttpStatusCode.OK, usuarioDto);

                return respuesta;
            });
        }


        /* [HttpPost]
         [Route("register")]
         public HttpResponseMessage Register(HttpRequestMessage request, UserViewModel user)
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
                     if (usersRepository.UserExists(user.Email))
                     {
                         ModelState.AddModelError("E-mail invalido","Ya existe un usuario con ese e-mail.");
                         response = request.CreateResponse(HttpStatusCode.BadRequest,
                         ModelState.Keys.SelectMany(k => ModelState[k].Errors)
                               .Select(m => m.ErrorMessage).ToArray());
                     }
                     else
                     {
                         User newUser = new User();
                         newUser = Mapper.Map<UserViewModel, User>(user);
                         usersRepository.Add(newUser);

                         _unitOfWork.Commit();

                         // Update view model
                         user = Mapper.Map<User, UserViewModel>(newUser);
                         response = request.CreateResponse<UserViewModel>(HttpStatusCode.Created, user);
                     }
                 }

                 return response;
             });
         }
         */
        [HttpPost]
        [Route("update")]
        public HttpResponseMessage Update(HttpRequestMessage pedido, UserDto usuarioDto)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                HttpResponseMessage respuesta = null;

                if (!ModelState.IsValid)
                {
                    respuesta = pedido.CreateResponse(HttpStatusCode.BadRequest,
                        ModelState.Keys.SelectMany(k => ModelState[k].Errors)
                              .Select(m => m.ErrorMessage).ToArray());
                }
                else
                {
                    //TODO: hacer esto en el ServicioUsuarios 
                    User usuario = repositorioUsuarios.ObtenerUnoPorEmail(usuarioDto.Email);
                    usuario.Email = usuarioDto.Email;

                    repositorioUsuarios.Editar(usuario);

                    _unidadDeTrabajo.GuardarCambios();

                    respuesta = pedido.CreateResponse(HttpStatusCode.OK);
                }

                return respuesta;
            });
        }

        /*  [HttpGet]
          [Route("search/{page:int=0}/{pageSize=4}/{filter?}")]
          public HttpResponseMessage Search(HttpRequestMessage request, int? page, int? pageSize, string filter = null)
          {
              int currentPage = page.Value;
              int currentPageSize = pageSize.Value;

              return CreateHttpResponse(request, () =>
              {
                  HttpResponseMessage response = null;
                  List<Customer> customers = null;
                  int totalCustomers = new int();

                  if (!string.IsNullOrEmpty(filter))
                  {
                      filter = filter.Trim().ToLower();

                      customers = _customersRepository.FindBy(c => c.LastName.ToLower().Contains(filter) ||
                              c.IdentityCard.ToLower().Contains(filter) ||
                              c.FirstName.ToLower().Contains(filter))
                          .OrderBy(c => c.ID)
                          .Skip(currentPage * currentPageSize)
                          .Take(currentPageSize)
                          .ToList();

                      totalCustomers = _customersRepository.GetAll()
                          .Where(c => c.LastName.ToLower().Contains(filter) ||
                              c.IdentityCard.ToLower().Contains(filter) ||
                              c.FirstName.ToLower().Contains(filter))
                          .Count();
                  }
                  else
                  {
                      customers = _customersRepository.GetAll()
                          .OrderBy(c => c.ID)
                          .Skip(currentPage * currentPageSize)
                          .Take(currentPageSize)
                      .ToList();

                      totalCustomers = _customersRepository.GetAll().Count();
                  }

                  IEnumerable<UserViewModel> customersVM = Mapper.Map<IEnumerable<Customer>, IEnumerable<UserViewModel>>(customers);

                  PaginationSet<UserViewModel> pagedSet = new PaginationSet<UserViewModel>()
                  {
                      Page = currentPage,
                      TotalCount = totalCustomers,
                      TotalPages = (int)Math.Ceiling((decimal)totalCustomers / currentPageSize),
                      Items = customersVM
                  };

                  response = request.CreateResponse<PaginationSet<UserViewModel>>(HttpStatusCode.OK, pagedSet);

                  return response;
              });
          }*/
    }
}
