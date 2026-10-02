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
    public class ImagesController : ControladorApiBase
    {
        private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".gif" };
        private const long TamanoMaximoBytes = 5 * 1024 * 1024;

        private readonly IRepositorioEntidadBase<Pet> repositorioMascotas;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;

        public ImagesController(IRepositorioEntidadBase<Pet> repositorioMascotas,
                                 IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                 IRepositorioEntidadBase<Customer> repositorioClientes,
                                 IRepositorioEntidadBase<Error> repositorioErrores,
                                 IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.repositorioMascotas = repositorioMascotas;
            this.repositorioPaseadores = repositorioPaseadores;
            this.repositorioClientes = repositorioClientes;
        }

        [HttpPost]
        [Route("pet/{id}")]
        public async Task<HttpResponseMessage> UploadPetImage(HttpRequestMessage pedido, Int64 id)
        {
            try
            {
                var mascota = repositorioMascotas.ObtenerUno(id);
                if (mascota == null)
                    return pedido.CreateResponse(HttpStatusCode.NotFound, "Mascota no encontrada.");

                var rutaImagen = await GuardarImagenAsync(pedido, "pets");
                if (rutaImagen == null)
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, "Debe subir una imagen válida (jpg, jpeg, png o gif) de hasta 5 MB.");

                EliminarImagenSiExiste(mascota.ProfileImage);
                mascota.ProfileImage = rutaImagen;
                _unidadDeTrabajo.GuardarCambios();

                return pedido.CreateResponse(HttpStatusCode.OK, new { ProfileImage = rutaImagen });
            }
            catch (Exception ex)
            {
                RegistrarError(ex);
                return pedido.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        [Route("walker/{id}")]
        public async Task<HttpResponseMessage> UploadWalkerImage(HttpRequestMessage pedido, Int64 id)
        {
            try
            {
                var paseador = repositorioPaseadores.ObtenerUno(id);
                if (paseador == null)
                    return pedido.CreateResponse(HttpStatusCode.NotFound, "Paseador no encontrado.");

                var rutaImagen = await GuardarImagenAsync(pedido, "walkers");
                if (rutaImagen == null)
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, "Debe subir una imagen válida (jpg, jpeg, png o gif) de hasta 5 MB.");

                EliminarImagenSiExiste(paseador.ProfileImage);
                paseador.ProfileImage = rutaImagen;
                _unidadDeTrabajo.GuardarCambios();

                return pedido.CreateResponse(HttpStatusCode.OK, new { ProfileImage = rutaImagen });
            }
            catch (Exception ex)
            {
                RegistrarError(ex);
                return pedido.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        [Route("customer/{id}")]
        public async Task<HttpResponseMessage> UploadCustomerImage(HttpRequestMessage pedido, Int64 id)
        {
            try
            {
                var cliente = repositorioClientes.ObtenerUno(id);
                if (cliente == null)
                    return pedido.CreateResponse(HttpStatusCode.NotFound, "Cliente no encontrado.");

                var rutaImagen = await GuardarImagenAsync(pedido, "customers");
                if (rutaImagen == null)
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, "Debe subir una imagen válida (jpg, jpeg, png o gif) de hasta 5 MB.");

                EliminarImagenSiExiste(cliente.ProfileImage);
                cliente.ProfileImage = rutaImagen;
                _unidadDeTrabajo.GuardarCambios();

                return pedido.CreateResponse(HttpStatusCode.OK, new { ProfileImage = rutaImagen });
            }
            catch (Exception ex)
            {
                RegistrarError(ex);
                return pedido.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private async Task<string> GuardarImagenAsync(HttpRequestMessage pedido, string subcarpeta)
        {
            if (!pedido.Content.IsMimeMultipartContent())
                return null;

            var carpetaSubidas = HttpContext.Current.Server.MapPath("~/Content/images/uploads/" + subcarpeta);
            Directory.CreateDirectory(carpetaSubidas);

            var proveedor = new MultipartFormDataStreamProvider(carpetaSubidas);
            await pedido.Content.ReadAsMultipartAsync(proveedor);

            var archivo = proveedor.FileData.FirstOrDefault();
            if (archivo == null || archivo.Headers.ContentDisposition == null || string.IsNullOrEmpty(archivo.Headers.ContentDisposition.FileName))
            {
                if (archivo != null) EliminarSinErrores(archivo.LocalFileName);
                return null;
            }

            var nombreOriginalArchivo = archivo.Headers.ContentDisposition.FileName.Trim('"');
            var extension = Path.GetExtension(nombreOriginalArchivo).ToLowerInvariant();

            if (!ExtensionesPermitidas.Contains(extension))
            {
                EliminarSinErrores(archivo.LocalFileName);
                return null;
            }

            var infoArchivo = new FileInfo(archivo.LocalFileName);
            if (infoArchivo.Length == 0 || infoArchivo.Length > TamanoMaximoBytes)
            {
                EliminarSinErrores(archivo.LocalFileName);
                return null;
            }

            var nombreFinalArchivo = Guid.NewGuid().ToString("N") + extension;
            var rutaFinal = Path.Combine(carpetaSubidas, nombreFinalArchivo);
            File.Move(archivo.LocalFileName, rutaFinal);

            return "/Content/images/uploads/" + subcarpeta + "/" + nombreFinalArchivo;
        }

        private void EliminarImagenSiExiste(string rutaRelativa)
        {
            if (string.IsNullOrEmpty(rutaRelativa) || !rutaRelativa.StartsWith("/Content/images/uploads/"))
                return;

            EliminarSinErrores(HttpContext.Current.Server.MapPath("~" + rutaRelativa));
        }

        private void EliminarSinErrores(string ruta)
        {
            try
            {
                if (File.Exists(ruta)) File.Delete(ruta);
            }
            catch { }
        }
    }
}
