using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    [RoutePrefix("api/images")]
    public class ImagesController : ApiControllerBase
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private readonly IEntityBaseRepository<Pet> petsRepository;
        private readonly IEntityBaseRepository<Walker> walkersRepository;
        private readonly IEntityBaseRepository<Customer> customersRepository;

        public ImagesController(IEntityBaseRepository<Pet> petsRepository,
                                 IEntityBaseRepository<Walker> walkersRepository,
                                 IEntityBaseRepository<Customer> customersRepository,
                                 IEntityBaseRepository<Error> errorsRepository,
                                 IUnitOfWork unitOfWork)
            : base(errorsRepository, unitOfWork)
        {
            this.petsRepository = petsRepository;
            this.walkersRepository = walkersRepository;
            this.customersRepository = customersRepository;
        }

        [HttpPost]
        [Route("pet/{id}")]
        public async Task<HttpResponseMessage> UploadPetImage(HttpRequestMessage request, Int64 id)
        {
            try
            {
                var pet = petsRepository.GetSingle(id);
                if (pet == null)
                    return request.CreateResponse(HttpStatusCode.NotFound, "Mascota no encontrada.");

                var imagePath = await SaveImageAsync(request, "pets");
                if (imagePath == null)
                    return request.CreateResponse(HttpStatusCode.BadRequest, "Debe subir una imagen válida (jpg, jpeg, png o gif) de hasta 5 MB.");

                DeleteImageIfExists(pet.ProfileImage);
                pet.ProfileImage = imagePath;
                _unitOfWork.Commit();

                return request.CreateResponse(HttpStatusCode.OK, new { ProfileImage = imagePath });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        [Route("walker/{id}")]
        public async Task<HttpResponseMessage> UploadWalkerImage(HttpRequestMessage request, Int64 id)
        {
            try
            {
                var walker = walkersRepository.GetSingle(id);
                if (walker == null)
                    return request.CreateResponse(HttpStatusCode.NotFound, "Paseador no encontrado.");

                var imagePath = await SaveImageAsync(request, "walkers");
                if (imagePath == null)
                    return request.CreateResponse(HttpStatusCode.BadRequest, "Debe subir una imagen válida (jpg, jpeg, png o gif) de hasta 5 MB.");

                DeleteImageIfExists(walker.ProfileImage);
                walker.ProfileImage = imagePath;
                _unitOfWork.Commit();

                return request.CreateResponse(HttpStatusCode.OK, new { ProfileImage = imagePath });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        [Route("customer/{id}")]
        public async Task<HttpResponseMessage> UploadCustomerImage(HttpRequestMessage request, Int64 id)
        {
            try
            {
                var customer = customersRepository.GetSingle(id);
                if (customer == null)
                    return request.CreateResponse(HttpStatusCode.NotFound, "Cliente no encontrado.");

                var imagePath = await SaveImageAsync(request, "customers");
                if (imagePath == null)
                    return request.CreateResponse(HttpStatusCode.BadRequest, "Debe subir una imagen válida (jpg, jpeg, png o gif) de hasta 5 MB.");

                DeleteImageIfExists(customer.ProfileImage);
                customer.ProfileImage = imagePath;
                _unitOfWork.Commit();

                return request.CreateResponse(HttpStatusCode.OK, new { ProfileImage = imagePath });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private async Task<string> SaveImageAsync(HttpRequestMessage request, string subFolder)
        {
            if (!request.Content.IsMimeMultipartContent())
                return null;

            var uploadsFolder = HttpContext.Current.Server.MapPath("~/Content/images/uploads/" + subFolder);
            Directory.CreateDirectory(uploadsFolder);

            var provider = new MultipartFormDataStreamProvider(uploadsFolder);
            await request.Content.ReadAsMultipartAsync(provider);

            var file = provider.FileData.FirstOrDefault();
            if (file == null || file.Headers.ContentDisposition == null || string.IsNullOrEmpty(file.Headers.ContentDisposition.FileName))
            {
                if (file != null) SafeDelete(file.LocalFileName);
                return null;
            }

            var originalFileName = file.Headers.ContentDisposition.FileName.Trim('"');
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                SafeDelete(file.LocalFileName);
                return null;
            }

            var fileInfo = new FileInfo(file.LocalFileName);
            if (fileInfo.Length == 0 || fileInfo.Length > MaxFileSizeBytes)
            {
                SafeDelete(file.LocalFileName);
                return null;
            }

            var finalFileName = Guid.NewGuid().ToString("N") + extension;
            var finalPath = Path.Combine(uploadsFolder, finalFileName);
            File.Move(file.LocalFileName, finalPath);

            return "/Content/images/uploads/" + subFolder + "/" + finalFileName;
        }

        private void DeleteImageIfExists(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || !relativePath.StartsWith("/Content/images/uploads/"))
                return;

            SafeDelete(HttpContext.Current.Server.MapPath("~" + relativePath));
        }

        private void SafeDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }
    }
}
