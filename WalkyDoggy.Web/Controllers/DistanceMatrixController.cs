using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    public class DistanceMatrixController : ApiControllerBase
    {
        private readonly IDistanceMatrixAppService distanceMatrixAppService;

        public DistanceMatrixController(IDistanceMatrixAppService distanceMatrixAppService,
                                        IEntityBaseRepository<Error> errorsRepository,
                                        IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.distanceMatrixAppService = distanceMatrixAppService;
        }
    }
}