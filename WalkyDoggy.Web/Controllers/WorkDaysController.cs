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
using WalkyDoggy.Entities.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/workDays")]
    public class WorkDaysController : ApiControllerBase
    {
        private readonly IWorkDayAppService workDayAppService;

        public WorkDaysController(IWorkDayAppService workDayAppService,
                                 IEntityBaseRepository<Error> errorsRepository,
                                 IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.workDayAppService = workDayAppService;
        }

        [HttpGet]
        [Route("getAll")]
        public HttpResponseMessage GetAll(HttpRequestMessage request)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var workDaysDto = this.workDayAppService.GetAll();
                response = request.CreateResponse(HttpStatusCode.OK, workDaysDto);
                return response;
            });
        }

        [HttpGet]
        [Route("getAllByWalkerId")]
        public HttpResponseMessage GetAllByWalkerId(HttpRequestMessage request, Int64 walkerId)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var workDaysDto = this.workDayAppService.GetAllByWalkerId(walkerId);
                response = request.CreateResponse(HttpStatusCode.OK, workDaysDto);
                return response;
            });
        }

        [HttpGet]
        [Route("getById")]
        public HttpResponseMessage GetById(HttpRequestMessage request, Int64 id)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var workDaysDto = this.workDayAppService.GetById(id);
                response = request.CreateResponse(HttpStatusCode.OK, workDaysDto);
                return response;
            });
        }

        [HttpPost]
        [Route("save")]
        public HttpResponseMessage Save(HttpRequestMessage request, WorkDayDto workDayDto)
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
                    var workDay = this.workDayAppService.Save(workDayDto);
                    if (workDay != null)
                    {
                        response = request.CreateResponse<WorkDay>(HttpStatusCode.OK, workDay);
                    }
                    else
                    {
                        response = request.CreateResponse(HttpStatusCode.InternalServerError, "Ya existe una jornada laboral con los valores seleccionados.");
                    }

                }

                return response;
            });
        }

        public HttpResponseMessage Delete(HttpRequestMessage request, Int64 id)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                this.workDayAppService.Delete(id);
                response = request.CreateResponse(HttpStatusCode.OK);
                return response;
            });
        }
    }
}