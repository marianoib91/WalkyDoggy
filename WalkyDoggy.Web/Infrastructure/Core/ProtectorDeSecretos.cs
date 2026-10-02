using System;
using System.Text;
using System.Web;
using System.Web.Security;

namespace WalkyDoggy.Web.Infrastructure.Core
{
    //Cifra y descifra textos con la machineKey del servidor. Sirve para guardar tokens en la base sin dejarlos en texto plano
    //y para firmar valores que van y vuelven por el navegador (por ejemplo el "state" de OAuth).
    //Ojo: si cambia la machineKey del servidor, lo cifrado antes ya no se puede leer (el paseador tendria que volver a vincular su cuenta).
    public static class ProtectorDeSecretos
    {
        public static String Proteger(String texto, String purpose)
        {
            var bytes = MachineKey.Protect(Encoding.UTF8.GetBytes(texto), purpose);
            return Convert.ToBase64String(bytes);
        }

        //Devuelve null si el texto fue alterado, es de otro proposito o no se puede descifrar
        public static String Desproteger(String textoProtegido, String purpose)
        {
            try
            {
                var bytes = MachineKey.Unprotect(Convert.FromBase64String(textoProtegido), purpose);
                return bytes == null ? null : Encoding.UTF8.GetString(bytes);
            }
            catch (Exception)
            {
                return null;
            }
        }

        //Version para valores que viajan en una URL
        public static String ProtegerParaUrl(String texto, String purpose)
        {
            return HttpServerUtility.UrlTokenEncode(MachineKey.Protect(Encoding.UTF8.GetBytes(texto), purpose));
        }

        public static String DesprotegerDeUrl(String token, String purpose)
        {
            try
            {
                var bytes = HttpServerUtility.UrlTokenDecode(token);
                if (bytes == null)
                {
                    return null;
                }

                var textoPlano = MachineKey.Unprotect(bytes, purpose);
                return textoPlano == null ? null : Encoding.UTF8.GetString(textoPlano);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
