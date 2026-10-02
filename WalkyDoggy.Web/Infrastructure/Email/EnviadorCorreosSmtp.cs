using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using WalkyDoggy.Services.Contracts;

namespace WalkyDoggy.Web.Infrastructure.Email
{
    //Manda los mails por SMTP. Si no hay un servidor configurado (Smtp.Host vacio) los deja como archivos .eml en
    //App_Data\outbox, para poder ver los avisos durante el desarrollo sin mandar nada a nadie.
    //Config (Secrets.config): Smtp.Host, Smtp.Port, Smtp.User, Smtp.Password, Smtp.From, Smtp.EnableSsl, App.BaseUrl.
    public class EnviadorCorreosSmtp : IEnviadorCorreos
    {
        private const String RemitentePorDefecto = "WalkyDoggy <no-reply@walkydoggy.local>";

        public String AppUrl
        {
            get { return (LeerConfiguracion("App.BaseUrl") ?? "http://localhost:8085").TrimEnd('/'); }
        }

        public void Enviar(String to, String asunto, String cuerpo)
        {
            try
            {
                var host = LeerConfiguracion("Smtp.Host");
                var usarSmtp = !String.IsNullOrWhiteSpace(host);
                var carpetaSalida = usarSmtp ? null : CarpetaDeSalida();

                var mensaje = new MailMessage
                {
                    From = new MailAddress(LeerConfiguracion("Smtp.From") ?? RemitentePorDefecto),
                    Subject = asunto,
                    SubjectEncoding = Encoding.UTF8,
                    Body = cuerpo,
                    BodyEncoding = Encoding.UTF8,
                    IsBodyHtml = false
                };
                mensaje.To.Add(to);

                var clienteSmtp = usarSmtp ? ArmarClienteSmtp(host) : ArmarClienteDeCarpeta(carpetaSalida);

                //Se manda en segundo plano: un servidor de mail lento no tiene que demorar la respuesta de la API
                Task.Run(() =>
                {
                    try
                    {
                        clienteSmtp.Send(mensaje);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("No se pudo enviar el mail \"" + asunto + "\": " + ex.GetType().Name + " " + ex.Message);
                    }
                    finally
                    {
                        mensaje.Dispose();
                        clienteSmtp.Dispose();
                    }
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudo preparar el mail \"" + asunto + "\": " + ex.GetType().Name + " " + ex.Message);
            }
        }

        private static SmtpClient ArmarClienteSmtp(String host)
        {
            int puerto;
            if (!Int32.TryParse(LeerConfiguracion("Smtp.Port"), out puerto))
            {
                puerto = 587;
            }

            var clienteSmtp = new SmtpClient(host, puerto)
            {
                EnableSsl = !String.Equals(LeerConfiguracion("Smtp.EnableSsl"), "false", StringComparison.OrdinalIgnoreCase),
                Timeout = 15000
            };

            var usuario = LeerConfiguracion("Smtp.User");
            if (!String.IsNullOrWhiteSpace(usuario))
            {
                clienteSmtp.Credentials = new NetworkCredential(usuario, LeerConfiguracion("Smtp.Password"));
            }

            return clienteSmtp;
        }

        private static SmtpClient ArmarClienteDeCarpeta(String carpeta)
        {
            return new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = carpeta
            };
        }

        private static String CarpetaDeSalida()
        {
            var carpeta = Path.Combine(HostingEnvironment.MapPath("~/App_Data"), "outbox");
            Directory.CreateDirectory(carpeta);
            return carpeta;
        }

        private static String LeerConfiguracion(String clave)
        {
            var valor = ConfigurationManager.AppSettings[clave];
            return String.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }
    }
}
