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
    public class MercadoPagoPaymentGateway : IPaymentGateway
    {
        private static readonly HttpClient Http = new HttpClient();

        private readonly MercadoPagoSettings settings;

        public MercadoPagoPaymentGateway()
        {
            this.settings = MercadoPagoSettings.FromConfig();
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public String CreateCheckout(String sellerAccessToken, CheckoutRequest request, out String error)
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

            //Comision de WalkyDoggy (opcional): Mercado Pago se la deriva a la cuenta de la aplicacion
            if (settings.CommissionPercent > 0)
            {
                body.Add("marketplace_fee", Math.Round(request.Amount * settings.CommissionPercent / 100m, 2));
            }

            var response = Send(HttpMethod.Post, "/checkout/preferences", sellerAccessToken, body, out error);
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

        public GatewayPayment GetPayment(String sellerAccessToken, String paymentId, out String error)
        {
            long id;
            if (!Int64.TryParse(paymentId, NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                error = "El número de pago no es válido.";
                return null;
            }

            var response = Send(HttpMethod.Get, "/v1/payments/" + id, sellerAccessToken, null, out error);
            return response == null ? null : ToPayment(response);
        }

        public GatewayPayment FindApprovedPayment(String sellerAccessToken, String externalReference, Decimal expectedAmount, out String error)
        {
            var path = "/v1/payments/search?sort=date_created&criteria=desc&external_reference=" + Uri.EscapeDataString(externalReference);
            var response = Send(HttpMethod.Get, path, sellerAccessToken, null, out error);
            if (response == null)
            {
                return null;
            }

            //Entre los pagos aprobados de la reserva se prefiere el de monto correcto (por si se pago de mas de una vez)
            GatewayPayment firstApproved = null;
            var results = response["results"] as JArray;
            if (results != null)
            {
                foreach (var result in results)
                {
                    if ((String)result["status"] != "approved")
                    {
                        continue;
                    }

                    var payment = ToPayment((JObject)result);
                    if (payment.Amount == expectedAmount)
                    {
                        return payment;
                    }

                    firstApproved = firstApproved ?? payment;
                }
            }

            return firstApproved;
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
        private JObject Send(HttpMethod method, String path, String accessToken, JObject body, out String error)
        {
            error = null;

            try
            {
                var result = Task.Run(() => SendAsync(method, path, accessToken, body)).GetAwaiter().GetResult();
                if (result.Item1 < 200 || result.Item1 >= 300)
                {
                    error = result.Item1 == 401
                        ? "Mercado Pago no aceptó la cuenta del paseador. Que la vincule de nuevo desde su perfil."
                        : "Mercado Pago respondió con un error (HTTP " + result.Item1 + ").";
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

        private async Task<Tuple<int, String>> SendAsync(HttpMethod method, String path, String accessToken, JObject body)
        {
            using (var message = new HttpRequestMessage(method, settings.ApiUrl + path))
            {
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
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
