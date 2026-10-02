using System;
using System.Text;
using System.Web;
using System.Web.Security;

namespace WalkyDoggy.Web.Infrastructure.Core
{
    //Cifra y descifra textos con la machineKey del servidor. Sirve para guardar tokens en la base sin dejarlos en texto plano
    //y para firmar valores que van y vuelven por el navegador (por ejemplo el "state" de OAuth).
    //Ojo: si cambia la machineKey del servidor, lo cifrado antes ya no se puede leer (el paseador tendria que volver a vincular su cuenta).
    public static class SecretProtector
    {
        public static String Protect(String text, String purpose)
        {
            var bytes = MachineKey.Protect(Encoding.UTF8.GetBytes(text), purpose);
            return Convert.ToBase64String(bytes);
        }

        //Devuelve null si el texto fue alterado, es de otro proposito o no se puede descifrar
        public static String Unprotect(String protectedText, String purpose)
        {
            try
            {
                var bytes = MachineKey.Unprotect(Convert.FromBase64String(protectedText), purpose);
                return bytes == null ? null : Encoding.UTF8.GetString(bytes);
            }
            catch (Exception)
            {
                return null;
            }
        }

        //Version para valores que viajan en una URL
        public static String ProtectForUrl(String text, String purpose)
        {
            return HttpServerUtility.UrlTokenEncode(MachineKey.Protect(Encoding.UTF8.GetBytes(text), purpose));
        }

        public static String UnprotectFromUrl(String token, String purpose)
        {
            try
            {
                var bytes = HttpServerUtility.UrlTokenDecode(token);
                if (bytes == null)
                {
                    return null;
                }

                var plain = MachineKey.Unprotect(bytes, purpose);
                return plain == null ? null : Encoding.UTF8.GetString(plain);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
