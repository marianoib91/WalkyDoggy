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
    public class ServicioNotificaciones : IServicioNotificaciones
    {
        private static readonly CultureInfo CulturaEspanola = new CultureInfo("es-AR");

        private readonly IRepositorioEntidadBase<Walk> repositorioPaseos;
        private readonly IEnviadorCorreos enviadorCorreos;

        public ServicioNotificaciones(IRepositorioEntidadBase<Walk> repositorioPaseos, IEnviadorCorreos enviadorCorreos)
        {
            this.repositorioPaseos = repositorioPaseos;
            this.enviadorCorreos = enviadorCorreos;
        }

        //Los datos de una reserva que se usan para armar los mails
        private class Reserva
        {
            public List<Walk> Walks { get; set; }
            public String WalkerName { get; set; }
            public String WalkerFirstName { get; set; }
            public String WalkerEmail { get; set; }
            public String CustomerName { get; set; }
            public String CustomerFirstName { get; set; }
            public String CustomerEmail { get; set; }
            public String Pets { get; set; }
            public String When { get; set; }
            public String Total { get; set; }
            public Boolean PaysWithMercadoPago { get; set; }
        }

        public void ReservaSolicitada(IEnumerable<Walk> paseos)
        {
            Notificar(paseos, reserva =>
            {
                var pago = reserva.PaysWithMercadoPago
                    ? "Mercado Pago (el cliente te paga online cuando termines el paseo)"
                    : "Efectivo (te paga en mano al terminar)";

                EnviarA(reserva.WalkerEmail, "Nueva solicitud de paseo de " + reserva.CustomerName,
                    "Hola " + reserva.WalkerFirstName + ",\n\n" +
                    reserva.CustomerName + " te solicitó un paseo para " + reserva.Pets + " el " + reserva.When + ".\n" +
                    "Pago: " + pago + " · " + reserva.Total + "\n\n" +
                    "Confirmá o rechazá la solicitud acá: " + ArmarEnlace("#/") + "\n\n" +
                    "Si no respondés antes del horario del paseo, la solicitud se cancela sola.");
            });
        }

        public void ReservaConfirmada(IEnumerable<Walk> paseos)
        {
            Notificar(paseos, reserva =>
            {
                var pago = reserva.PaysWithMercadoPago
                    ? "Cuando el paseador termine el paseo vas a poder pagarlo online desde \"Paseos solicitados\"."
                    : "Le pagás al paseador en mano cuando termina el paseo.";

                EnviarA(reserva.CustomerEmail, reserva.WalkerName + " confirmó tu paseo",
                    "Hola " + reserva.CustomerFirstName + ",\n\n" +
                    reserva.WalkerName + " confirmó el paseo de " + reserva.Pets + " el " + reserva.When + " (" + reserva.Total + ").\n\n" +
                    pago + "\n\nVer tus paseos: " + ArmarEnlace("#/walks/requested"));
            });
        }

        public void ReservaCancelada(IEnumerable<Walk> paseos, String canceladoPor)
        {
            Notificar(paseos, reserva =>
            {
                var detalle = reserva.Pets + " el " + reserva.When;

                if (canceladoPor == WalkCancelledBy.Walker)
                {
                    EnviarA(reserva.CustomerEmail, reserva.WalkerName + " canceló tu paseo",
                        "Hola " + reserva.CustomerFirstName + ",\n\n" + reserva.WalkerName + " canceló el paseo de " + detalle + "." +
                        "\n\nPodés pedirle el paseo a otro paseador: " + ArmarEnlace("#/"));
                }
                else if (canceladoPor == WalkCancelledBy.Customer)
                {
                    EnviarA(reserva.WalkerEmail, reserva.CustomerName + " canceló un paseo",
                        "Hola " + reserva.WalkerFirstName + ",\n\n" + reserva.CustomerName + " canceló el paseo de " + detalle + ". El horario quedó libre.");
                }
                else
                {
                    EnviarA(reserva.CustomerEmail, "Tu solicitud de paseo no fue respondida",
                        "Hola " + reserva.CustomerFirstName + ",\n\n" + reserva.WalkerName + " no respondió a tiempo tu solicitud para " + detalle +
                        ", por eso se canceló.\n\nPodés pedirle el paseo a otro paseador: " + ArmarEnlace("#/"));
                    EnviarA(reserva.WalkerEmail, "Una solicitud de paseo venció sin respuesta",
                        "Hola " + reserva.WalkerFirstName + ",\n\nLa solicitud de " + reserva.CustomerName + " para " + detalle +
                        " se canceló porque no la respondiste antes del horario.");
                }
            });
        }

        /* ---------- Armado de los mails ---------- */

        private void Notificar(IEnumerable<Walk> paseos, Action<Reserva> send)
        {
            try
            {
                foreach (var reserva in CargarReservas(paseos))
                {
                    try
                    {
                        send(reserva);
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

        private List<Reserva> CargarReservas(IEnumerable<Walk> paseos)
        {
            var ids = paseos.Select(x => x.Id).ToList();
            if (ids.Count == 0)
            {
                return new List<Reserva>();
            }

            var cargado = this.repositorioPaseos.TodosConIncluidos(x => x.Pet, x => x.Pet.Customer, x => x.Pet.Customer.User,
                                                           x => x.Walker, x => x.Walker.User, x => x.Price).
                                              Where(x => ids.Contains(x.Id)).
                                              ToList();

            return cargado.GroupBy(AyudanteReservas.ClaveDe).Select(group =>
            {
                var primero = group.First();
                var cliente = primero.Pet.Customer;
                var paseador = primero.Walker;
                var inicio = AyudanteReservas.InicioDe(primero);
                var total = group.Sum(x => x.Price != null ? x.Price.Amount : 0);

                return new Reserva
                {
                    Walks = group.ToList(),
                    WalkerName = paseador.FirstName + " " + paseador.LastName,
                    WalkerFirstName = paseador.FirstName,
                    WalkerEmail = paseador.User != null ? paseador.User.Email : null,
                    CustomerName = cliente.FirstName + " " + cliente.LastName,
                    CustomerFirstName = cliente.FirstName,
                    CustomerEmail = cliente.User != null ? cliente.User.Email : null,
                    Pets = String.Join(", ", group.Select(x => x.Pet.Name)),
                    When = inicio.ToString("dddd d 'de' MMMM", CulturaEspanola) + ", de " + inicio.ToString("HH:mm", CulturaEspanola) +
                           " a " + inicio.AddHours(1).ToString("HH:mm", CulturaEspanola),
                    Total = "$" + total.ToString("N0", CulturaEspanola),
                    PaysWithMercadoPago = primero.PaymentMethod == PaymentMethods.MercadoPago
                };
            }).ToList();
        }

        private void EnviarA(String email, String asunto, String cuerpo)
        {
            if (String.IsNullOrWhiteSpace(email))
            {
                return;
            }

            this.enviadorCorreos.Enviar(email, asunto, cuerpo + "\n\n— WalkyDoggy");
        }

        private String ArmarEnlace(String ruta)
        {
            return this.enviadorCorreos.AppUrl + "/" + ruta;
        }
    }
}
