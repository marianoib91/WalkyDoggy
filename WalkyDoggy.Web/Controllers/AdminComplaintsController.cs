using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Controllers
{
    //El administrador revisa y resuelve las denuncias (ver ControladorAdminBase: todas las acciones exigen ser administrador)
    [RoutePrefix("api/admin/complaints")]
    public class AdminComplaintsController : ControladorAdminBase
    {
        private readonly IServicioDenuncias servicioDenuncias;

        public AdminComplaintsController(IServicioDenuncias servicioDenuncias,
                                         IRepositorioEntidadBase<User> repositorioUsuarios,
                                         IRepositorioEntidadBase<Error> repositorioErrores,
                                         IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioUsuarios, repositorioErrores, unidadDeTrabajo)
        {
            this.servicioDenuncias = servicioDenuncias;
        }

        [HttpGet]
        [Route("")]
        public HttpResponseMessage List(HttpRequestMessage pedido, String status = null, Int32 page = 1, Int32 pageSize = 15)
        {
            return ComoAdministrador(pedido, idAdministrador =>
                pedido.CreateResponse(HttpStatusCode.OK, servicioDenuncias.Listar(status, page, pageSize)));
        }

        [HttpGet]
        [Route("{id:long}")]
        public HttpResponseMessage Get(HttpRequestMessage pedido, Int64 id)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                var denuncia = servicioDenuncias.Obtener(id);
                return denuncia == null
                    ? pedido.CreateResponse(HttpStatusCode.NotFound, new[] { "La denuncia no existe." })
                    : pedido.CreateResponse(HttpStatusCode.OK, denuncia);
            });
        }

        [HttpPost]
        [Route("status")]
        public HttpResponseMessage SetStatus(HttpRequestMessage pedido, SetComplaintStatusDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioDenuncias.PonerEnRevision(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        [HttpPost]
        [Route("resolve")]
        public HttpResponseMessage Resolve(HttpRequestMessage pedido, ResolveComplaintDto solicitud)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var salioBien = servicioDenuncias.Resolver(idAdministrador, solicitud, out error);
                return Resultado(pedido, salioBien, error);
            });
        }

        //La imagen se pide con la cabecera Authorization (la pantalla la baja como archivo y la muestra): no hay un enlace publico
        [HttpGet]
        [Route("{id:long}/images/{imageId:long}")]
        public HttpResponseMessage Image(HttpRequestMessage pedido, Int64 id, Int64 imageId)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                var imagen = servicioDenuncias.ObtenerImagen(id, imageId);
                if (imagen == null)
                {
                    return pedido.CreateResponse(HttpStatusCode.NotFound, new[] { "La imagen no existe." });
                }

                var ruta = Path.Combine(HttpContext.Current.Server.MapPath(ComplaintsController.CarpetaImagenes), imagen.FileName);
                if (!File.Exists(ruta))
                {
                    return pedido.CreateResponse(HttpStatusCode.NotFound, new[] { "El archivo de la imagen ya no está." });
                }

                var respuesta = pedido.CreateResponse(HttpStatusCode.OK);
                respuesta.Content = new ByteArrayContent(File.ReadAllBytes(ruta));
                respuesta.Content.Headers.ContentType = new MediaTypeHeaderValue(imagen.ContentType);
                respuesta.Headers.CacheControl = new CacheControlHeaderValue { NoStore = true, Private = true };
                return respuesta;
            });
        }

        //Leer el chat de la reserva denunciada: cada lectura queda en la bitacora
        [HttpGet]
        [Route("{id:long}/chat")]
        public HttpResponseMessage Chat(HttpRequestMessage pedido, Int64 id)
        {
            return ComoAdministrador(pedido, idAdministrador =>
            {
                String error;
                var mensajes = servicioDenuncias.LeerChat(idAdministrador, id, out error);
                return mensajes == null
                    ? pedido.CreateResponse(HttpStatusCode.NotFound, new[] { error })
                    : pedido.CreateResponse(HttpStatusCode.OK, mensajes);
            });
        }
    }
}
