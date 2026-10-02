using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Services.Utilities;

namespace WalkyDoggy.Services.Services
{
    public class NotificationAppService : INotificationAppService
    {
        private static readonly CultureInfo Spanish = new CultureInfo("es-AR");

        private readonly IEntityBaseRepository<Walk> walksRepository;
        private readonly IEmailSender emailSender;

        public NotificationAppService(IEntityBaseRepository<Walk> walksRepository, IEmailSender emailSender)
        {
            this.walksRepository = walksRepository;
            this.emailSender = emailSender;
        }

        //Los datos de una reserva que se usan para armar los mails
        private class Booking
        {
            public List<Walk> Walks { get; set; }
            public String WalkerName { get; set; }
            public String WalkerFirstName { get; set; }
            public String WalkerEmail { get; set; }
            public String WalkerPayoutAccount { get; set; }
            public String WalkerPayoutHolder { get; set; }
            public String CustomerName { get; set; }
            public String CustomerFirstName { get; set; }
            public String CustomerEmail { get; set; }
            public String Pets { get; set; }
            public String When { get; set; }
            public String Total { get; set; }
            public Boolean PaysWithMercadoPago { get; set; }
            public String PaymentStatus { get; set; }
            public String DisputeReason { get; set; }
        }

        public void BookingRequested(IEnumerable<Walk> walks)
        {
            Notify(walks, booking =>
            {
                var payment = booking.PaysWithMercadoPago
                    ? "Mercado Pago (el cliente paga cuando confirmes el paseo)"
                    : "Efectivo (te paga en mano al terminar)";

                SendTo(booking.WalkerEmail, "Nueva solicitud de paseo de " + booking.CustomerName,
                    "Hola " + booking.WalkerFirstName + ",\n\n" +
                    booking.CustomerName + " te solicitó un paseo para " + booking.Pets + " el " + booking.When + ".\n" +
                    "Pago: " + payment + " · " + booking.Total + "\n\n" +
                    "Confirmá o rechazá la solicitud acá: " + Link("#/") + "\n\n" +
                    "Si no respondés antes del horario del paseo, la solicitud se cancela sola.");
            });
        }

        public void BookingConfirmed(IEnumerable<Walk> walks)
        {
            Notify(walks, booking =>
            {
                var next = booking.PaysWithMercadoPago
                    ? "Para asegurarlo, pagalo desde \"Paseos solicitados\" antes del horario: " + Link("#/walks/requested") + "\n" +
                      "WalkyDoggy retiene el pago y se lo libera al paseador cuando el paseo se hace; si no se hace, te lo devolvemos.\n" +
                      "Si no lo pagás a tiempo, el paseo se cancela."
                    : "Le pagás al paseador en mano cuando termina el paseo.\n\nVer tus paseos: " + Link("#/walks/requested");

                SendTo(booking.CustomerEmail, booking.WalkerName + " confirmó tu paseo",
                    "Hola " + booking.CustomerFirstName + ",\n\n" +
                    booking.WalkerName + " confirmó el paseo de " + booking.Pets + " el " + booking.When + " (" + booking.Total + ").\n\n" + next);
            });
        }

        public void BookingCancelled(IEnumerable<Walk> walks, String cancelledBy)
        {
            Notify(walks, booking =>
            {
                var refunded = booking.PaymentStatus == PaymentStatuses.Refunded
                    ? "\n\nComo ya habías pagado, te devolvimos el pago de " + booking.Total + "."
                    : String.Empty;
                var detail = booking.Pets + " el " + booking.When;

                if (cancelledBy == WalkCancelledBy.Walker)
                {
                    SendTo(booking.CustomerEmail, booking.WalkerName + " canceló tu paseo",
                        "Hola " + booking.CustomerFirstName + ",\n\n" + booking.WalkerName + " canceló el paseo de " + detail + "." + refunded +
                        "\n\nPodés pedirle el paseo a otro paseador: " + Link("#/"));
                }
                else if (cancelledBy == WalkCancelledBy.Customer)
                {
                    SendTo(booking.WalkerEmail, booking.CustomerName + " canceló un paseo",
                        "Hola " + booking.WalkerFirstName + ",\n\n" + booking.CustomerName + " canceló el paseo de " + detail + ". El horario quedó libre.");
                }
                else if (cancelledBy == WalkCancelledBy.NoPayment)
                {
                    SendTo(booking.CustomerEmail, "Se canceló tu paseo por falta de pago",
                        "Hola " + booking.CustomerFirstName + ",\n\nEl paseo de " + detail + " con " + booking.WalkerName +
                        " se canceló porque no se pagó antes del horario.\n\nPodés volver a reservar: " + Link("#/"));
                    SendTo(booking.WalkerEmail, "Se canceló un paseo por falta de pago",
                        "Hola " + booking.WalkerFirstName + ",\n\nEl paseo de " + detail + " con " + booking.CustomerName +
                        " se canceló porque el cliente no pagó antes del horario. El horario quedó libre.");
                }
                else
                {
                    SendTo(booking.CustomerEmail, "Tu solicitud de paseo no fue respondida",
                        "Hola " + booking.CustomerFirstName + ",\n\n" + booking.WalkerName + " no respondió a tiempo tu solicitud para " + detail +
                        ", por eso se canceló.\n\nPodés pedirle el paseo a otro paseador: " + Link("#/"));
                    SendTo(booking.WalkerEmail, "Una solicitud de paseo venció sin respuesta",
                        "Hola " + booking.WalkerFirstName + ",\n\nLa solicitud de " + booking.CustomerName + " para " + detail +
                        " se canceló porque no la respondiste antes del horario.");
                }
            });
        }

        public void PaymentHeld(IEnumerable<Walk> walks)
        {
            Notify(walks, booking =>
            {
                SendTo(booking.CustomerEmail, "Recibimos tu pago de " + booking.Total,
                    "Hola " + booking.CustomerFirstName + ",\n\nRecibimos tu pago de " + booking.Total + " por el paseo de " + booking.Pets +
                    " con " + booking.WalkerName + " el " + booking.When + ".\n\n" +
                    "Lo retenemos hasta que el paseo se haga. Después le liberamos el pago al paseador; si no se hace, te lo devolvemos.\n" +
                    "Cuando termine, confirmá que salió bien (o reportá un problema) desde: " + Link("#/walks/requested"));
                SendTo(booking.WalkerEmail, booking.CustomerName + " pagó el paseo",
                    "Hola " + booking.WalkerFirstName + ",\n\n" + booking.CustomerName + " pagó " + booking.Total + " por el paseo de " + booking.Pets +
                    " el " + booking.When + ".\n\nWalkyDoggy retiene el pago y te lo liquida cuando el paseo se hace." + PayoutNote(booking));
            });
        }

        public void PaymentReleased(IEnumerable<Walk> walks)
        {
            Notify(walks, booking =>
            {
                SendTo(booking.WalkerEmail, "Se liberó el pago de un paseo",
                    "Hola " + booking.WalkerFirstName + ",\n\nSe liberó el pago de " + booking.Total + " por el paseo de " + booking.Pets +
                    " el " + booking.When + " (" + booking.CustomerName + ").\n\nWalkyDoggy te lo liquida a la brevedad. Ver tus cobros: " + Link("#/") +
                    PayoutNote(booking));
            });
        }

        public void PaymentDisputed(IEnumerable<Walk> walks)
        {
            Notify(walks, booking =>
            {
                SendTo(booking.WalkerEmail, booking.CustomerName + " reportó un problema con un paseo",
                    "Hola " + booking.WalkerFirstName + ",\n\n" + booking.CustomerName + " reportó un problema con el paseo de " + booking.Pets +
                    " el " + booking.When + ".\n\nMotivo: " + booking.DisputeReason + "\n\n" +
                    "El pago de " + booking.Total + " queda retenido mientras se revisa.");
                SendTo(booking.CustomerEmail, "Recibimos tu reclamo",
                    "Hola " + booking.CustomerFirstName + ",\n\nRecibimos tu reclamo por el paseo de " + booking.Pets + " con " + booking.WalkerName +
                    " el " + booking.When + ". El pago de " + booking.Total + " queda retenido y no se le libera al paseador mientras lo revisamos.");
            });
        }

        /* ---------- Armado de los mails ---------- */

        private void Notify(IEnumerable<Walk> walks, Action<Booking> send)
        {
            try
            {
                foreach (var booking in LoadBookings(walks))
                {
                    try
                    {
                        send(booking);
                    }
                    catch (Exception)
                    {
                        //Un mail que no se puede armar no tiene que impedir los demas
                    }
                }
            }
            catch (Exception)
            {
                //Los avisos nunca rompen la operacion que los origino
            }
        }

        private List<Booking> LoadBookings(IEnumerable<Walk> walks)
        {
            var ids = walks.Select(x => x.Id).ToList();
            if (ids.Count == 0)
            {
                return new List<Booking>();
            }

            var loaded = this.walksRepository.AllIncluding(x => x.Pet, x => x.Pet.Customer, x => x.Pet.Customer.User,
                                                           x => x.Walker, x => x.Walker.User, x => x.Price).
                                              Where(x => ids.Contains(x.Id)).
                                              ToList();

            return loaded.GroupBy(BookingHelper.KeyOf).Select(group =>
            {
                var first = group.First();
                var customer = first.Pet.Customer;
                var walker = first.Walker;
                var start = BookingHelper.StartOf(first);
                var total = group.Sum(x => x.Price != null ? x.Price.Amount : 0);

                return new Booking
                {
                    Walks = group.ToList(),
                    WalkerName = walker.FirstName + " " + walker.LastName,
                    WalkerFirstName = walker.FirstName,
                    WalkerEmail = walker.User != null ? walker.User.Email : null,
                    WalkerPayoutAccount = walker.PayoutAccount,
                    WalkerPayoutHolder = walker.PayoutHolder,
                    CustomerName = customer.FirstName + " " + customer.LastName,
                    CustomerFirstName = customer.FirstName,
                    CustomerEmail = customer.User != null ? customer.User.Email : null,
                    Pets = String.Join(", ", group.Select(x => x.Pet.Name)),
                    When = start.ToString("dddd d 'de' MMMM", Spanish) + ", de " + start.ToString("HH:mm", Spanish) +
                           " a " + start.AddHours(1).ToString("HH:mm", Spanish),
                    Total = "$" + total.ToString("N0", Spanish),
                    PaysWithMercadoPago = first.PaymentMethod == PaymentMethods.MercadoPago,
                    PaymentStatus = first.PaymentStatus,
                    DisputeReason = group.Select(x => x.PaymentDisputeReason).FirstOrDefault(x => x != null)
                };
            }).ToList();
        }

        private void SendTo(String email, String subject, String body)
        {
            if (String.IsNullOrWhiteSpace(email))
            {
                return;
            }

            this.emailSender.Send(email, subject, body + "\n\n— WalkyDoggy");
        }

        //Adonde se le va a transferir al paseador, o el pedido de que cargue su cuenta si todavia no lo hizo
        private String PayoutNote(Booking booking)
        {
            if (String.IsNullOrEmpty(booking.WalkerPayoutAccount))
            {
                return "\n\nTodavía no cargaste dónde cobrar. Completá tu alias o CBU/CVU en tu perfil para que podamos transferirte: " + Link("#/profile");
            }

            return "\n\nTe lo transferimos a " + booking.WalkerPayoutAccount + " (titular: " + booking.WalkerPayoutHolder + "). Si no es correcto, corregilo en tu perfil: " + Link("#/profile");
        }

        private String Link(String path)
        {
            return this.emailSender.AppUrl + "/" + path;
        }
    }
}
