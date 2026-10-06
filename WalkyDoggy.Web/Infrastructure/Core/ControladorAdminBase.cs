using System;
using System.Net;
using System.Net.Http;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Web.Infrastructure.Core
{
    //Base de los controladores de administracion: ninguna accion se ejecuta si quien llama no inicio sesion con una cuenta de administrador.
    //No alcanza con esconder el menu en la pantalla.
    public abstract class ControladorAdminBase : ControladorApiBase
    {
        private readonly IRepositorioEntidadBase<User> repositorioUsuarios;

        protected ControladorAdminBase(IRepositorioEntidadBase<User> repositorioUsuarios,
                                       IRepositorioEntidadBase<Error> repositorioErrores,
                                       IUnidadDeTrabajo unidadDeTrabajo)
            : base(repositorioErrores, unidadDeTrabajo)
        {
            this.repositorioUsuarios = repositorioUsuarios;
        }

        //Ejecuta la accion solo si quien llama es administrador; si no, responde 401 (sin sesion) o 403 (no es administrador)
        protected HttpResponseMessage ComoAdministrador(HttpRequestMessage pedido, Func<Int64, HttpResponseMessage> accion)
        {
            return CrearRespuestaHttp(pedido, () =>
            {
                if (!IdentidadDelActor.EstaAutenticado(User))
                {
                    return pedido.CreateResponse(HttpStatusCode.Unauthorized, new[] { "Tenés que iniciar sesión." });
                }

                Int64 idAdministrador;
                if (!IdentidadDelActor.EsAdministrador(User, repositorioUsuarios, out idAdministrador))
                {
                    return pedido.CreateResponse(HttpStatusCode.Forbidden, new[] { "Esta función es solo para administradores." });
                }

                return accion(idAdministrador);
            });
        }

        //Para las acciones que no pueden usar ComoAdministrador (por ejemplo, las que leen un archivo del cuerpo del pedido de forma asincronica)
        protected Boolean EsAdministrador(out Int64 idAdministrador)
        {
            return IdentidadDelActor.EsAdministrador(User, repositorioUsuarios, out idAdministrador);
        }

        //Respuesta de una accion que puede fallar con un mensaje para mostrar: 200 si salio bien, 400 con el motivo si no
        protected static HttpResponseMessage Resultado(HttpRequestMessage pedido, Boolean salioBien, String error)
        {
            return salioBien
                ? pedido.CreateResponse(HttpStatusCode.OK, true)
                : pedido.CreateResponse(HttpStatusCode.BadRequest, new[] { error });
        }
    }
}
