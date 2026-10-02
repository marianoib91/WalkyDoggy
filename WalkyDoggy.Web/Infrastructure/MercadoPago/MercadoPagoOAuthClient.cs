using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WalkyDoggy.Web.Infrastructure.MercadoPago
{
    public class MercadoPagoTokens
    {
        public String UserId { get; set; }

        public String AccessToken { get; set; }

        public String RefreshToken { get; set; }

        public String PublicKey { get; set; }

        public DateTime ExpiresAtUtc { get; set; }
    }

    //Intercambia el codigo de autorizacion que devuelve Mercado Pago por los tokens de la cuenta del paseador.
    //Documentacion: POST https://api.mercadopago.com/oauth/token (Content-Type: application/json)
    public class MercadoPagoOAuthClient
    {
        private static readonly HttpClient Http = new HttpClient();

        private readonly MercadoPagoSettings settings;

        public MercadoPagoOAuthClient(MercadoPagoSettings settings)
        {
            this.settings = settings;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        //URL a la que se manda al paseador para que autorice la vinculacion
        public String BuildAuthorizationUrl(String state)
        {
            return settings.AuthorizationUrl +
                   "?client_id=" + Uri.EscapeDataString(settings.ClientId) +
                   "&response_type=code" +
                   "&platform_id=mp" +
                   "&state=" + Uri.EscapeDataString(state) +
                   "&redirect_uri=" + Uri.EscapeDataString(settings.RedirectUri);
        }

        public async Task<MercadoPagoTokens> ExchangeCodeAsync(String code)
        {
            var body = new JObject
            {
                { "client_id", settings.ClientId },
                { "client_secret", settings.ClientSecret },
                { "code", code },
                { "grant_type", "authorization_code" },
                { "redirect_uri", settings.RedirectUri }
            };

            if (settings.TestMode)
            {
                body.Add("test_token", "true");
            }

            return await RequestTokensAsync(body, "la vinculación");
        }

        //Los tokens vencen (a los 180 dias): con el refresh token se piden unos nuevos sin que el paseador vuelva a autorizar
        public async Task<MercadoPagoTokens> RefreshAsync(String refreshToken)
        {
            var body = new JObject
            {
                { "client_id", settings.ClientId },
                { "client_secret", settings.ClientSecret },
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            };

            return await RequestTokensAsync(body, "la renovación del acceso");
        }

        private async Task<MercadoPagoTokens> RequestTokensAsync(JObject body, String what)
        {
            var response = await Http.PostAsync(settings.TokenUrl, new StringContent(body.ToString(), Encoding.UTF8, "application/json")).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                //No se incluye el cuerpo completo para no volcar datos sensibles en el log de errores
                throw new InvalidOperationException("Mercado Pago rechazó " + what + " (HTTP " + (int)response.StatusCode + ").");
            }

            var json = JObject.Parse(content);
            var accessToken = (String)json["access_token"];
            if (String.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Mercado Pago no devolvió un token de acceso.");
            }

            var expiresIn = json["expires_in"] != null ? (Int64)json["expires_in"] : 0;

            return new MercadoPagoTokens
            {
                UserId = (String)json["user_id"],
                AccessToken = accessToken,
                RefreshToken = (String)json["refresh_token"],
                PublicKey = (String)json["public_key"],
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn)
            };
        }
    }
}
