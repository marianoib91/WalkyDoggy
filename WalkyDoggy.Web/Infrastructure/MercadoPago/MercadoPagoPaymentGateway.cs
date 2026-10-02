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
    //Crea, consulta y reembolsa pagos en Mercado Pago con el Access Token de la cuenta de WalkyDoggy (Checkout Pro).
    //Documentacion: POST /checkout/preferences, GET /v1/payments/{id}, GET /v1/payments/search, POST /v1/payments/{id}/refunds
    public class MercadoPagoPaymentGateway : IPaymentGateway
    {
        private static readonly HttpClient Http = new HttpClient();

        private readonly MercadoPagoSettings settings;

        public MercadoPagoPaymentGateway()
        {
            this.settings = MercadoPagoSettings.FromConfig();
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public Boolean IsConfigured
        {
            get { return settings.PaymentsConfigured; }
        }

        public String CreateCheckout(CheckoutRequest request, out String error)
        {
            var returnUrl = request.ReturnUrl;
            var body = new JObject
            {
                { "items", new JArray(new JObject
                    {
                        { "id", request.ExternalReference },
                        { "title", request.Title },
                        { "quantity", 1 },
                        { "currency_id", "ARS" },
                        { "unit_price", request.Amount }
                    }) },
                { "external_reference", request.ExternalReference },
                { "back_urls", new JObject { { "success", returnUrl }, { "failure", returnUrl }, { "pending", returnUrl } } },
                { "statement_descriptor", "WALKYDOGGY" }
            };

            //La vuelta automatica solo se pide con https: Mercado Pago puede rechazar una direccion local para esto
            if (returnUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                body.Add("auto_return", "approved");
            }

            var response = Send(HttpMethod.Post, "/checkout/preferences", body, null, out error);
            if (response == null)
            {
                return null;
            }

            var url = (String)response["init_point"];
            if (String.IsNullOrEmpty(url))
            {
                error = "Mercado Pago no devolvió la dirección de pago.";
                return null;
            }

            return url;
        }

        public GatewayPayment GetPayment(String paymentId, out String error)
        {
            long id;
            if (!Int64.TryParse(paymentId, NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                error = "El número de pago no es válido.";
                return null;
            }

            var response = Send(HttpMethod.Get, "/v1/payments/" + id, null, null, out error);
            return response == null ? null : ToPayment(response);
        }

        public GatewayPayment FindApprovedPayment(String externalReference, out String error)
        {
            var path = "/v1/payments/search?sort=date_created&criteria=desc&external_reference=" + Uri.EscapeDataString(externalReference);
            var response = Send(HttpMethod.Get, path, null, null, out error);
            if (response == null)
            {
                return null;
            }

            var results = response["results"] as JArray;
            if (results != null)
            {
                foreach (var result in results)
                {
                    if ((String)result["status"] == "approved")
                    {
                        return ToPayment((JObject)result);
                    }
                }
            }

            return null;
        }

        public Boolean Refund(String paymentId, out String error)
        {
            long id;
            if (!Int64.TryParse(paymentId, NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                error = "El número de pago no es válido.";
                return false;
            }

            //Sin monto se devuelve el pago completo. La clave de idempotencia evita devolverlo dos veces si se reintenta.
            return Send(HttpMethod.Post, "/v1/payments/" + id + "/refunds", new JObject(), "refund-" + id, out error) != null;
        }

        private static GatewayPayment ToPayment(JObject json)
        {
            return new GatewayPayment
            {
                Id = (String)json["id"],
                Status = (String)json["status"],
                ExternalReference = (String)json["external_reference"],
                Amount = json["transaction_amount"] != null ? (Decimal)json["transaction_amount"] : 0m,
                Currency = (String)json["currency_id"]
            };
        }

        //No se incluye el cuerpo de la respuesta del error para no volcar datos sensibles; solo el codigo HTTP
        private JObject Send(HttpMethod method, String path, JObject body, String idempotencyKey, out String error)
        {
            error = null;

            if (!settings.PaymentsConfigured)
            {
                error = "Los pagos con Mercado Pago todavía no están configurados en el sistema.";
                return null;
            }

            try
            {
                var result = Task.Run(() => SendAsync(method, path, body, idempotencyKey)).GetAwaiter().GetResult();
                if (result.Item1 < 200 || result.Item1 >= 300)
                {
                    error = "Mercado Pago respondió con un error (HTTP " + result.Item1 + ").";
                    return null;
                }

                return String.IsNullOrWhiteSpace(result.Item2) ? new JObject() : JObject.Parse(result.Item2);
            }
            catch (Exception ex)
            {
                error = "No se pudo comunicar con Mercado Pago. Intentá nuevamente. (" + ex.GetType().Name + ")";
                return null;
            }
        }

        private async Task<Tuple<int, String>> SendAsync(HttpMethod method, String path, JObject body, String idempotencyKey)
        {
            using (var message = new HttpRequestMessage(method, settings.ApiUrl + path))
            {
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
                if (idempotencyKey != null)
                {
                    message.Headers.Add("X-Idempotency-Key", idempotencyKey);
                }
                if (body != null)
                {
                    message.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
                }

                using (var response = await Http.SendAsync(message).ConfigureAwait(false))
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return Tuple.Create((int)response.StatusCode, content);
                }
            }
        }
    }
}
