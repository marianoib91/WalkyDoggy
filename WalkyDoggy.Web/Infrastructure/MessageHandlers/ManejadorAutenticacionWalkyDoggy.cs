using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using WalkyDoggy.Web.Infrastructure.Extensions;
using System.Text;
using System.Net;

namespace WalkyDoggy.Web.MessageHandlers
{
    public class ManejadorAutenticacionWalkyDoggy : DelegatingHandler
    {
        IEnumerable<string> valoresEncabezadoAuth = null;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage pedido, CancellationToken cancellationToken)
        {
            try
            {
                pedido.Headers.TryGetValues("Authorization",out valoresEncabezadoAuth);
                if(valoresEncabezadoAuth == null)
                    return base.SendAsync(pedido, cancellationToken); // sin credenciales se deja pasar el pedido y cada accion decide si exige autenticacion

                var tokens = valoresEncabezadoAuth.FirstOrDefault();
                tokens = tokens.Replace("Basic","").Trim();
                if (!string.IsNullOrEmpty(tokens))
                {
                    byte[] datos = Convert.FromBase64String(tokens);
                    string textoDecodificado = Encoding.UTF8.GetString(datos);
                    string[] valoresTokens = textoDecodificado.Split(':');
                    var servicioMembresia = pedido.ObtenerServicioMembresia();

                    var contextoMembresia = servicioMembresia.ValidarUsuario(valoresTokens[0], valoresTokens[1]);
                    if (contextoMembresia.User != null)
                    {
                        IPrincipal principal = contextoMembresia.Principal;
                        Thread.CurrentPrincipal = principal;
                        HttpContext.Current.User = principal;
                    }
                    else // Unauthorized access - wrong crededentials
                    {
                        var respuesta = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                        var tsc = new TaskCompletionSource<HttpResponseMessage>();
                        tsc.SetResult(respuesta);
                        return tsc.Task;
                    }
                }
                else
                {
                    var respuesta = new HttpResponseMessage(HttpStatusCode.Forbidden);
                    var tsc = new TaskCompletionSource<HttpResponseMessage>();
                    tsc.SetResult(respuesta);
                    return tsc.Task;
                }
                return base.SendAsync(pedido, cancellationToken);
            }
            catch
            {
                var respuesta = new HttpResponseMessage(HttpStatusCode.Forbidden);
                var tsc = new TaskCompletionSource<HttpResponseMessage>();
                tsc.SetResult(respuesta);
                return tsc.Task;
            }
        }
    }
}