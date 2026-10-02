using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WalkyDoggy.Web.Infrastructure.MercadoPago
{
    public class TokensMercadoPago
    {
        public String UserId { get; set; }

        public String AccessToken { get; set; }

        public String RefreshToken { get; set; }

        public String PublicKey { get; set; }

        public DateTime ExpiresAtUtc { get; set; }
    }

    //Intercambia el codigo de autorizacion que devuelve Mercado Pago por los tokens de la cuenta del paseador.
    //Documentacion: POST https://api.mercadopago.com/oauth/token (Content-Type: application/json)
    public class ClienteOAuthMercadoPago
    {
        private static readonly HttpClient Http = new HttpClient();

        private readonly ConfiguracionMercadoPago configuracion;

        public ClienteOAuthMercadoPago(ConfiguracionMercadoPago configuracion)
        {
            this.configuracion = configuracion;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        //URL a la que se manda al paseador para que autorice la vinculacion
        public String ArmarUrlDeAutorizacion(String state)
        {
            return configuracion.AuthorizationUrl +
                   "?client_id=" + Uri.EscapeDataString(configuracion.ClientId) +
                   "&response_type=code" +
                   "&platform_id=mp" +
                   "&state=" + Uri.EscapeDataString(state) +
                   "&redirect_uri=" + Uri.EscapeDataString(configuracion.RedirectUri);
        }

        public async Task<TokensMercadoPago> CanjearCodigoAsync(String codigo)
        {
            var cuerpo = new JObject
            {
                { "client_id", configuracion.ClientId },
                { "client_secret", configuracion.ClientSecret },
                { "code", codigo },
                { "grant_type", "authorization_code" },
                { "redirect_uri", configuracion.RedirectUri }
            };

            if (configuracion.TestMode)
            {
                cuerpo.Add("test_token", "true");
            }

            return await PedirTokensAsync(cuerpo, "la vinculación");
        }

        //Los tokens vencen (a los 180 dias): con el refresh token se piden unos nuevos sin que el paseador vuelva a autorizar
        public async Task<TokensMercadoPago> RenovarAsync(String tokenDeRenovacion)
        {
            var cuerpo = new JObject
            {
                { "client_id", configuracion.ClientId },
                { "client_secret", configuracion.ClientSecret },
                { "grant_type", "refresh_token" },
                { "refresh_token", tokenDeRenovacion }
            };

            return await PedirTokensAsync(cuerpo, "la renovación del acceso");
        }

        private async Task<TokensMercadoPago> PedirTokensAsync(JObject cuerpo, String what)
        {
            var respuesta = await Http.PostAsync(configuracion.TokenUrl, new StringContent(cuerpo.ToString(), Encoding.UTF8, "application/json")).ConfigureAwait(false);
            var contenido = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!respuesta.IsSuccessStatusCode)
            {
                //No se incluye el cuerpo completo para no volcar datos sensibles en el log de errores
                throw new InvalidOperationException("Mercado Pago rechazó " + what + " (HTTP " + (int)respuesta.StatusCode + ").");
            }

            var json = JObject.Parse(contenido);
            var tokenDeAcceso = (String)json["access_token"];
            if (String.IsNullOrEmpty(tokenDeAcceso))
            {
                throw new InvalidOperationException("Mercado Pago no devolvió un token de acceso.");
            }

            var venceEn = json["expires_in"] != null ? (Int64)json["expires_in"] : 0;

            return new TokensMercadoPago
            {
                UserId = (String)json["user_id"],
                AccessToken = tokenDeAcceso,
                RefreshToken = (String)json["refresh_token"],
                PublicKey = (String)json["public_key"],
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(venceEn)
            };
        }
    }
}
