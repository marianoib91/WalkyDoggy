using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Services.Services;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    //Publicidad: el administrador carga los comercios (pet shops, veterinarias, etc.) y sus avisos
    //(ver ControladorAdminBase: todas las acciones exigen ser administrador)
    [RoutePrefix("api/admin/ads")]
    public class AdminAdsController : ControladorAdminBase
    {
        private const String CarpetaImagenes = "~/Content/images/uploads/ads";

        private readonly IServicioPublicidad servicioPublicidad;

        public AdminAdsController(IServicioPublicidad servicioPublicidad,
                                  IRepositorioEntidadBase<User> repositorioUsuarios,
                                  IRepositorioEntidadBase<Error> repositorioErrores,
                                  IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioUsuarios, repositorioErrores, unidadDeTrabajo)
        {
            this.servicioPublicidad = servicioPublicidad;
        }

        /* ---------- Comercios ---------- */

        [HttpGet]
        [Route("advertisers")]
        public HttpResponseMessage Advertisers(HttpRequestMessage pedido)
        {
            return ComoAdministrador(pedido, idAdministrador => pedido.CreateResponse(HttpStatusCode.OK, servicioPublicidad.ListarComercios()));
        }

        [HttpPost]
        [Route("advertisers/save")]
        public HttpResponseMessage SaveAdvertiser(HttpRequestMessage pedido, SaveAdvertiserDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioPublicidad.GuardarComercio(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        [HttpPost]
        [Route("advertisers/setActive")]
        public HttpResponseMessage SetAdvertiserActive(HttpRequestMessage pedido, SetCatalogItemActiveDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioPublicidad.EstablecerComercioActivo(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        /* ---------- Avisos ---------- */

        [HttpGet]
        [Route("")]
        public HttpResponseMessage List(HttpRequestMessage pedido)
        {
            return ComoAdministrador(pedido, idAdministrador => pedido.CreateResponse(HttpStatusCode.OK, servicioPublicidad.ListarAvisos()));
        }

        //Devuelve el id del aviso (nuevo o editado), para poder subirle la imagen a continuacion
        [HttpPost]
        [Route("save")]
        public HttpResponseMessage Save(HttpRequestMessage pedido, SaveAdDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                Int64 idAviso;
                return servicioPublicidad.GuardarAviso(idAdministrador, solicitud, out idAviso, out error)
                    ? pedido.CreateResponse(HttpStatusCode.OK, new { id = idAviso })
                    : pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
            });
        }

        [HttpPost]
        [Route("setActive")]
        public HttpResponseMessage SetActive(HttpRequestMessage pedido, SetCatalogItemActiveDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioPublicidad.EstablecerAvisoActivo(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        /* ---------- Imagen del aviso ---------- */

        //Formulario multipart con un solo archivo (jpg, png, gif o webp, hasta 5 MB). Reemplaza la imagen que tenia el aviso.
        [HttpPost]
        [Route("{id:long}/image")]
        public async Task<HttpResponseMessage> UploadImage(HttpRequestMessage pedido, Int64 id)
        {
            //Sin ser administrador no se llega a leer el cuerpo (no se guarda nada en disco)
            Int64 idAdministrador;
            if (!IdentidadDelActor.EstaAutenticado(User))
            {
                return pedido.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
            }
            if (!EsAdministrador(out idAdministrador))
            {
                return pedido.CreateResponse(HttpStatusCode.Forbidden, new[] { "Esta función es solo para administradores." });
            }
            if (!pedido.Content.IsMimeMultipartContent())
            {
                return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "El formulario no es válido." });
            }

            var carpeta = HttpContext.Current.Server.MapPath(CarpetaImagenes);
            Directory.CreateDirectory(carpeta);
            var proveedor = new MultipartFormDataStreamProvider(carpeta);
            String guardado = null;
            var salioBien = false;

            try
            {
                await pedido.Content.ReadAsMultipartAsync(proveedor);

                var archivo = proveedor.FileData.FirstOrDefault(x => x.Headers.ContentDisposition != null &&
                                                                     !String.IsNullOrEmpty((x.Headers.ContentDisposition.FileName ?? String.Empty).Trim('"')));
                if (archivo == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "Elegí una imagen." });
                }

                var nombreOriginal = Path.GetFileName(archivo.Headers.ContentDisposition.FileName.Trim('"'));
                var extension = (Path.GetExtension(nombreOriginal) ?? String.Empty).ToLowerInvariant();
                var info = new FileInfo(archivo.LocalFileName);

                if (ArchivosDeImagen.TipoDeContenido(extension) == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "La imagen tiene que ser jpg, png, gif o webp." });
                }
                if (info.Length == 0 || info.Length > ArchivosDeImagen.TamanoMaximoBytes)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "La imagen supera los 5 MB o está vacía." });
                }
                if (!ArchivosDeImagen.EsImagenDeVerdad(archivo.LocalFileName, extension))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "El archivo no es una imagen válida." });
                }

                var nombreGuardado = Guid.NewGuid().ToString("N") + extension;
                File.Copy(archivo.LocalFileName, Path.Combine(carpeta, nombreGuardado));
                guardado = Path.Combine(carpeta, nombreGuardado);

                String error, anterior;
                if (!servicioPublicidad.CambiarImagen(idAdministrador, id, nombreGuardado, out anterior, out error))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                //La imagen anterior ya no se usa
                if (!String.IsNullOrEmpty(anterior))
                {
                    ArchivosDeImagen.EliminarSinErrores(Path.Combine(carpeta, anterior));
                }

                salioBien = true;
                return pedido.CreateResponse(HttpStatusCode.OK, new { imageUrl = ServicioPublicidad.RutaImagenes + nombreGuardado });
            }
            catch (Exception ex)
            {
                RegistrarError(ex);
                return pedido.CreateResponse(HttpStatusCode.InternalServerError, new[] { "No se pudo subir la imagen. Intentá de nuevo." });
            }
            finally
            {
                foreach (var temporal in proveedor.FileData.Select(x => x.LocalFileName))
                {
                    ArchivosDeImagen.EliminarSinErrores(temporal);
                }
                if (!salioBien)
                {
                    ArchivosDeImagen.EliminarSinErrores(guardado);
                }
            }
        }

        [HttpPost]
        [Route("{id:long}/image/remove")]
        public HttpResponseMessage RemoveImage(HttpRequestMessage pedido, Int64 id)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error, anterior;
                if (!servicioPublicidad.CambiarImagen(idAdministrador, id, null, out anterior, out error))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                if (!String.IsNullOrEmpty(anterior))
                {
                    ArchivosDeImagen.EliminarSinErrores(Path.Combine(HttpContext.Current.Server.MapPath(CarpetaImagenes), anterior));
                }
                return pedido.CreateResponse(HttpStatusCode.OK, true);
            });
        }
    }
}
