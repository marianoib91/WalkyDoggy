using System;
using System.Configuration;
using System.Globalization;

namespace WalkyDoggy.Web.Infrastructure.MercadoPago
{
    //Credenciales de la aplicacion de Mercado Pago (Mercado Pago Developers).
    //Se leen de Web.config / Secrets.config; el Client Secret nunca debe subirse a git ni enviarse al navegador.
    //Cada paseador vincula su propia cuenta (OAuth) y los clientes le pagan directamente a el.
    public class MercadoPagoSettings
    {
        public String ClientId { get; private set; }

        public String ClientSecret { get; private set; }

        //Debe ser exactamente la "Redirect URL" cargada en la aplicacion de Mercado Pago.
        //Mercado Pago no acepta localhost: se usa la pagina puente publicada en GitHub Pages (docs/mp-callback.html),
        //que reenvia al paseador a esta misma aplicacion.
        public String RedirectUri { get; private set; }

        public String AuthorizationUrl { get; private set; }

        public String TokenUrl { get; private set; }

        //Direccion de la API de pagos (se cambia solo para probar contra un Mercado Pago simulado)
        public String ApiUrl { get; private set; }

        //Con cuentas de prueba, Mercado Pago pide "test_token=true" al pedir el token para generar credenciales de sandbox
        public Boolean TestMode { get; private set; }

        //Comision de WalkyDoggy sobre cada pago, en porcentaje. 0 = no se cobra comision.
        public Decimal CommissionPercent { get; private set; }

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
            Decimal commission;
            if (!Decimal.TryParse(ConfigurationManager.AppSettings["MercadoPago.CommissionPercent"], NumberStyles.Number, CultureInfo.InvariantCulture, out commission) ||
                commission < 0 || commission >= 100)
            {
                commission = 0;
            }

            return new MercadoPagoSettings
            {
                ClientId = ConfigurationManager.AppSettings["MercadoPago.ClientId"],
                ClientSecret = ConfigurationManager.AppSettings["MercadoPago.ClientSecret"],
                RedirectUri = ConfigurationManager.AppSettings["MercadoPago.RedirectUri"],
                AuthorizationUrl = ConfigurationManager.AppSettings["MercadoPago.AuthorizationUrl"] ?? "https://auth.mercadopago.com/authorization",
                TokenUrl = ConfigurationManager.AppSettings["MercadoPago.TokenUrl"] ?? "https://api.mercadopago.com/oauth/token",
                ApiUrl = (ConfigurationManager.AppSettings["MercadoPago.ApiUrl"] ?? "https://api.mercadopago.com").TrimEnd('/'),
                TestMode = String.Equals(ConfigurationManager.AppSettings["MercadoPago.TestMode"], "true", StringComparison.OrdinalIgnoreCase),
                CommissionPercent = commission
            };
        }
    }
}
