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
    public class SmtpEmailSender : IEmailSender
    {
        private const String DefaultFrom = "WalkyDoggy <no-reply@walkydoggy.local>";

        public String AppUrl
        {
            get { return (Setting("App.BaseUrl") ?? "http://localhost:8085").TrimEnd('/'); }
        }

        public void Send(String to, String subject, String body)
        {
            try
            {
                var host = Setting("Smtp.Host");
                var useSmtp = !String.IsNullOrWhiteSpace(host);
                var outbox = useSmtp ? null : OutboxFolder();

                var message = new MailMessage
                {
                    From = new MailAddress(Setting("Smtp.From") ?? DefaultFrom),
                    Subject = subject,
                    SubjectEncoding = Encoding.UTF8,
                    Body = body,
                    BodyEncoding = Encoding.UTF8,
                    IsBodyHtml = false
                };
                message.To.Add(to);

                var client = useSmtp ? BuildSmtpClient(host) : BuildPickupClient(outbox);

                //Se manda en segundo plano: un servidor de mail lento no tiene que demorar la respuesta de la API
                Task.Run(() =>
                {
                    try
                    {
                        client.Send(message);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("No se pudo enviar el mail \"" + subject + "\": " + ex.GetType().Name + " " + ex.Message);
                    }
                    finally
                    {
                        message.Dispose();
                        client.Dispose();
                    }
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudo preparar el mail \"" + subject + "\": " + ex.GetType().Name + " " + ex.Message);
            }
        }

        private static SmtpClient BuildSmtpClient(String host)
        {
            int port;
            if (!Int32.TryParse(Setting("Smtp.Port"), out port))
            {
                port = 587;
            }

            var client = new SmtpClient(host, port)
            {
                EnableSsl = !String.Equals(Setting("Smtp.EnableSsl"), "false", StringComparison.OrdinalIgnoreCase),
                Timeout = 15000
            };

            var user = Setting("Smtp.User");
            if (!String.IsNullOrWhiteSpace(user))
            {
                client.Credentials = new NetworkCredential(user, Setting("Smtp.Password"));
            }

            return client;
        }

        private static SmtpClient BuildPickupClient(String folder)
        {
            return new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = folder
            };
        }

        private static String OutboxFolder()
        {
            var folder = Path.Combine(HostingEnvironment.MapPath("~/App_Data"), "outbox");
            Directory.CreateDirectory(folder);
            return folder;
        }

        private static String Setting(String key)
        {
            var value = ConfigurationManager.AppSettings[key];
            return String.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
