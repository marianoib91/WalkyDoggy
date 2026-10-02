using System;
using System.Configuration;

namespace WalkyDoggy.Web.Infrastructure.MercadoPago
{
    //Credenciales de la aplicacion de Mercado Pago (Mercado Pago Developers).
    //Se leen de Web.config / Secrets.config; el Client Secret nunca debe subirse a git ni enviarse al navegador.
    public class MercadoPagoSettings
    {
        public String ClientId { get; private set; }

        public String ClientSecret { get; private set; }

        //Debe ser exactamente la "Redirect URL" cargada en la aplicacion de Mercado Pago
        public String RedirectUri { get; private set; }

        public String AuthorizationUrl { get; private set; }

        public String TokenUrl { get; private set; }

        //Con cuentas de prueba, Mercado Pago pide "test_token=true" al pedir el token para generar credenciales de sandbox
        public Boolean TestMode { get; private set; }

        //Access Token de la cuenta de WalkyDoggy: con el se crean los pagos y los reembolsos. Es una clave privada.
        public String AccessToken { get; private set; }

        public String ApiUrl { get; private set; }

        //Los pagos entran a la cuenta de WalkyDoggy, por eso solo hace falta el Access Token
        public Boolean PaymentsConfigured
        {
            get { return !String.IsNullOrWhiteSpace(AccessToken); }
        }

        //La vinculacion de la cuenta de cada paseador (OAuth) quedo en desuso: los pagos los cobra WalkyDoggy.
        //Se conserva por si mas adelante se vuelve al modelo de cobro directo.
        public Boolean LinkingEnabled { get; private set; }

        public Boolean IsConfigured
        {
            get
            {
                return !String.IsNullOrWhiteSpace(ClientId) &&
                       !String.IsNullOrWhiteSpace(ClientSecret) &&
                       !String.IsNullOrWhiteSpace(RedirectUri);
            }
        }

        public static MercadoPagoSettings FromConfig()
        {
            return new MercadoPagoSettings
            {
                ClientId = ConfigurationManager.AppSettings["MercadoPago.ClientId"],
                ClientSecret = ConfigurationManager.AppSettings["MercadoPago.ClientSecret"],
                RedirectUri = ConfigurationManager.AppSettings["MercadoPago.RedirectUri"],
                AuthorizationUrl = ConfigurationManager.AppSettings["MercadoPago.AuthorizationUrl"] ?? "https://auth.mercadopago.com/authorization",
                TokenUrl = ConfigurationManager.AppSettings["MercadoPago.TokenUrl"] ?? "https://api.mercadopago.com/oauth/token",
                TestMode = String.Equals(ConfigurationManager.AppSettings["MercadoPago.TestMode"], "true", StringComparison.OrdinalIgnoreCase),
                AccessToken = ConfigurationManager.AppSettings["MercadoPago.AccessToken"],
                ApiUrl = (ConfigurationManager.AppSettings["MercadoPago.ApiUrl"] ?? "https://api.mercadopago.com").TrimEnd('/'),
                LinkingEnabled = String.Equals(ConfigurationManager.AppSettings["MercadoPago.LinkingEnabled"], "true", StringComparison.OrdinalIgnoreCase)
            };
        }
    }
}
