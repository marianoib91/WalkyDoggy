using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WalkyDoggy.Services.Contracts;

namespace WalkyDoggy.Web.Infrastructure.MercadoPago
{
    //Crea y consulta pagos en Mercado Pago (Checkout Pro) con el access token de la cuenta del paseador: el pago se crea
    //en la cuenta del paseador y el dinero le llega a el. Si se configura una comision, Mercado Pago la deriva a WalkyDoggy.
    //Documentacion: POST /checkout/preferences, GET /v1/payments/{id}, GET /v1/payments/search
    public class PasarelaDePagoMercadoPago : IPasarelaDePago
    {
        private static readonly HttpClient Http = new HttpClient();

        private readonly ConfiguracionMercadoPago configuracion;

        public PasarelaDePagoMercadoPago()
        {
            this.configuracion = ConfiguracionMercadoPago.DesdeConfiguracion();
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public String CrearCheckout(String tokenAccesoVendedor, SolicitudDeCheckout solicitud, out String error)
        {
            var urlDeRetorno = solicitud.ReturnUrl;
            var cuerpo = new JObject
            {
                { "items", new JArray(new JObject
                    {
                        { "id", solicitud.ExternalReference },
                        { "title", solicitud.Title },
                        { "quantity", 1 },
                        { "currency_id", "ARS" },
                        { "unit_price", solicitud.Amount }
                    }) },
                { "external_reference", solicitud.ExternalReference },
                { "back_urls", new JObject { { "success", urlDeRetorno }, { "failure", urlDeRetorno }, { "pending", urlDeRetorno } } },
                { "statement_descriptor", "WALKYDOGGY" }
            };

            //La vuelta automatica solo se pide con https: Mercado Pago puede rechazar una direccion local para esto
            if (urlDeRetorno.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                cuerpo.Add("auto_return", "approved");
            }

            //Comision de WalkyDoggy (opcional): Mercado Pago se la deriva a la cuenta de la aplicacion
            if (configuracion.CommissionPercent > 0)
            {
                cuerpo.Add("marketplace_fee", Math.Round(solicitud.Amount * configuracion.CommissionPercent / 100m, 2));
            }

            var respuesta = Enviar(HttpMethod.Post, "/checkout/preferences", tokenAccesoVendedor, cuerpo, out error);
            if (respuesta == null)
            {
                return null;
            }

            var url = (String)respuesta["init_point"];
            if (String.IsNullOrEmpty(url))
            {
                error = "Mercado Pago no devolvió la dirección de pago.";
                return null;
            }

            return url;
        }

        public PagoPasarela ObtenerPago(String tokenAccesoVendedor, String idPago, out String error)
        {
            long id;
            if (!Int64.TryParse(idPago, NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                error = "El número de pago no es válido.";
                return null;
            }

            var respuesta = Enviar(HttpMethod.Get, "/v1/payments/" + id, tokenAccesoVendedor, null, out error);
            return respuesta == null ? null : APago(respuesta);
        }

        public PagoPasarela BuscarPagoAprobado(String tokenAccesoVendedor, String referenciaExterna, Decimal montoEsperado, out String error)
        {
            var ruta = "/v1/payments/search?sort=date_created&criteria=desc&external_reference=" + Uri.EscapeDataString(referenciaExterna);
            var respuesta = Enviar(HttpMethod.Get, ruta, tokenAccesoVendedor, null, out error);
            if (respuesta == null)
            {
                return null;
            }

            //Entre los pagos aprobados de la reserva se prefiere el de monto correcto (por si se pago de mas de una vez)
            PagoPasarela primerAprobado = null;
            var resultados = respuesta["results"] as JArray;
            if (resultados != null)
            {
                foreach (var resultado in resultados)
                {
                    if ((String)resultado["status"] != "approved")
                    {
                        continue;
                    }

                    var pago = APago((JObject)resultado);
                    if (pago.Amount == montoEsperado)
                    {
                        return pago;
                    }

                    primerAprobado = primerAprobado ?? pago;
                }
            }

            return primerAprobado;
        }

        private static PagoPasarela APago(JObject json)
        {
            return new PagoPasarela
            {
                Id = (String)json["id"],
                Status = (String)json["status"],
                ExternalReference = (String)json["external_reference"],
                Amount = json["transaction_amount"] != null ? (Decimal)json["transaction_amount"] : 0m,
                Currency = (String)json["currency_id"]
            };
        }

        //No se incluye el cuerpo de la respuesta del error para no volcar datos sensibles; solo el codigo HTTP
        private JObject Enviar(HttpMethod metodo, String ruta, String tokenDeAcceso, JObject cuerpo, out String error)
        {
            error = null;

            try
            {
                var resultado = Task.Run(() => EnviarAsync(metodo, ruta, tokenDeAcceso, cuerpo)).GetAwaiter().GetResult();
                if (resultado.Item1 < 200 || resultado.Item1 >= 300)
                {
                    error = resultado.Item1 == 401
                        ? "Mercado Pago no aceptó la cuenta del paseador. Que la vincule de nuevo desde su perfil."
                        : "Mercado Pago respondió con un error (HTTP " + resultado.Item1 + ").";
                    return null;
                }

                return String.IsNullOrWhiteSpace(resultado.Item2) ? new JObject() : JObject.Parse(resultado.Item2);
            }
            catch (Exception ex)
            {
                error = "No se pudo comunicar con Mercado Pago. Intentá nuevamente. (" + ex.GetType().Name + ")";
                return null;
            }
        }

        private async Task<Tuple<int, String>> EnviarAsync(HttpMethod metodo, String ruta, String tokenDeAcceso, JObject cuerpo)
        {
            using (var mensaje = new HttpRequestMessage(metodo, configuracion.ApiUrl + ruta))
            {
                mensaje.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenDeAcceso);
                if (cuerpo != null)
                {
                    mensaje.Content = new StringContent(cuerpo.ToString(), Encoding.UTF8, "application/json");
                }

                using (var respuesta = await Http.SendAsync(mensaje).ConfigureAwait(false))
                {
                    var contenido = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return Tuple.Create((int)respuesta.StatusCode, contenido);
                }
            }
        }
    }
}
