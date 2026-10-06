using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    //Un cliente o un paseador denuncia a la otra parte de una reserva, con imagenes opcionales (capturas de pantalla, fotos).
    //Las imagenes se guardan en App_Data/complaints (carpeta que el sitio NO publica): solo las puede ver un administrador.
    [RoutePrefix("api/complaints")]
    public class ComplaintsController : ControladorApiBase
    {
        public const Int32 MaximoImagenes = 5;

        //Carpeta (dentro de App_Data) donde se guardan las imagenes de las denuncias
        public const String CarpetaImagenes = "~/App_Data/complaints";

        private readonly IServicioDenuncias servicioDenuncias;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;

        public ComplaintsController(IServicioDenuncias servicioDenuncias,
                                    IRepositorioEntidadBase<Customer> repositorioClientes,
                                    IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                    IRepositorioEntidadBase<Error> repositorioErrores,
                                    IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.servicioDenuncias = servicioDenuncias;
            this.repositorioClientes = repositorioClientes;
            this.repositorioPaseadores = repositorioPaseadores;
        }

        //Formulario multipart: bookingKey, actor (Customer | Walker), actorId, reason, description y hasta 5 archivos de imagen
        [HttpPost]
        [Route("create")]
        public async Task<HttpResponseMessage> Create(HttpRequestMessage pedido)
        {
            //Sin sesion no se llega a leer el cuerpo (no se guarda nada en disco)
            if (!IdentidadDelActor.EstaAutenticado(User))
            {
                return pedido.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
            }
            if (!pedido.Content.IsMimeMultipartContent())
            {
                return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { "El formulario no es válido." });
            }

            var carpeta = HttpContext.Current.Server.MapPath(CarpetaImagenes);
            Directory.CreateDirectory(carpeta);

            var proveedor = new MultipartFormDataStreamProvider(carpeta);
            var guardados = new List<String>();
            var salioBien = false;

            try
            {
                await pedido.Content.ReadAsMultipartAsync(proveedor);

                var solicitud = new CreateComplaintDto
                {
                    BookingKey = Campo(proveedor, "bookingKey"),
                    Actor = Campo(proveedor, "actor"),
                    Reason = Campo(proveedor, "reason"),
                    Description = Campo(proveedor, "description")
                };
                Int64 idActor;
                Int64.TryParse(Campo(proveedor, "actorId"), out idActor);
                solicitud.ActorId = idActor;

                //Solo puede denunciar quien inicio sesion como el cliente o el paseador de esa reserva
                var esActor = solicitud.Actor == WalkCancelledBy.Customer
                    ? IdentidadDelActor.EsCliente(User, repositorioClientes, solicitud.ActorId)
                    : solicitud.Actor == WalkCancelledBy.Walker && IdentidadDelActor.EsPaseador(User, repositorioPaseadores, solicitud.ActorId);
                if (!esActor)
                {
                    return pedido.CreateResponse(HttpStatusCode.Forbidden, new[] { "No podés hacer una denuncia en nombre de otra persona." });
                }

                String errorImagenes;
                var imagenes = GuardarImagenes(proveedor, carpeta, guardados, out errorImagenes);
                if (imagenes == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { errorImagenes });
                }

                String error;
                if (!servicioDenuncias.Crear(solicitud, imagenes, out error))
                {
                    return pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
                }

                salioBien = true;
                return pedido.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                RegistrarError(ex);
                return pedido.CreateResponse(HttpStatusCode.InternalServerError, new[] { "No se pudo enviar la denuncia. Intentá de nuevo." });
            }
            finally
            {
                //Los archivos temporales (y los ya renombrados, si algo fallo) no se dejan en disco
                foreach (var archivo in proveedor.FileData.Select(x => x.LocalFileName))
                {
                    ArchivosDeImagen.EliminarSinErrores(archivo);
                }
                if (!salioBien)
                {
                    guardados.ForEach(ArchivosDeImagen.EliminarSinErrores);
                }
            }
        }

        private static String Campo(MultipartFormDataStreamProvider proveedor, String nombre)
        {
            return proveedor.FormData[nombre];
        }

        //Controla cada archivo (extension, tamaño y que el contenido sea de verdad una imagen) y lo guarda con un nombre al azar.
        //Devuelve null y el motivo en error si alguno no sirve.
        private static List<ComplaintImageInfo> GuardarImagenes(MultipartFormDataStreamProvider proveedor, String carpeta, List<String> guardados, out String error)
        {
            error = null;
            var imagenes = new List<ComplaintImageInfo>();

            var archivos = proveedor.FileData.Where(x => x.Headers.ContentDisposition != null &&
                                                         !String.IsNullOrEmpty((x.Headers.ContentDisposition.FileName ?? String.Empty).Trim('"'))).ToList();
            if (archivos.Count > MaximoImagenes)
            {
                error = "Podés adjuntar hasta " + MaximoImagenes + " imágenes.";
                return null;
            }

            foreach (var archivo in archivos)
            {
                var nombreOriginal = Path.GetFileName(archivo.Headers.ContentDisposition.FileName.Trim('"'));
                var extension = (Path.GetExtension(nombreOriginal) ?? String.Empty).ToLowerInvariant();
                var info = new FileInfo(archivo.LocalFileName);

                if (ArchivosDeImagen.TipoDeContenido(extension) == null)
                {
                    error = "\"" + nombreOriginal + "\" no es una imagen válida (jpg, png, gif o webp).";
                    return null;
                }
                if (info.Length == 0 || info.Length > ArchivosDeImagen.TamanoMaximoBytes)
                {
                    error = "\"" + nombreOriginal + "\" supera los 5 MB o está vacía.";
                    return null;
                }
                if (!ArchivosDeImagen.EsImagenDeVerdad(archivo.LocalFileName, extension))
                {
                    error = "\"" + nombreOriginal + "\" no es una imagen válida.";
                    return null;
                }

                var nombreGuardado = Guid.NewGuid().ToString("N") + extension;
                File.Copy(archivo.LocalFileName, Path.Combine(carpeta, nombreGuardado));
                guardados.Add(Path.Combine(carpeta, nombreGuardado));

                imagenes.Add(new ComplaintImageInfo
                {
                    FileName = nombreGuardado,
                    OriginalName = nombreOriginal.Length > 200 ? nombreOriginal.Substring(nombreOriginal.Length - 200) : nombreOriginal,
                    ContentType = ArchivosDeImagen.TipoDeContenido(extension),
                    SizeBytes = (Int32)info.Length
                });
            }

            return imagenes;
        }
    }
}
