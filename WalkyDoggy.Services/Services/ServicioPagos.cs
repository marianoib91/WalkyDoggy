using System;
using System.Collections.Generic;
using System.Linq;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Services.Utilities;

namespace WalkyDoggy.Services.Services
{
    public class ServicioPagos : IServicioPagos
    {
        private readonly IRepositorioEntidadBase<Walk> repositorioPaseos;
        private readonly IRepositorioEntidadBase<Stay> repositorioHospedajes;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;
        private readonly IPasarelaDePago pasarela;
        private readonly IProveedorTokensVendedor proveedorTokens;

        public ServicioPagos(IRepositorioEntidadBase<Walk> repositorioPaseos,
                                 IRepositorioEntidadBase<Stay> repositorioHospedajes,
                                 IUnidadDeTrabajo unidadDeTrabajo,
                                 IPasarelaDePago pasarela,
                                 IProveedorTokensVendedor proveedorTokens)
        {
            this.repositorioPaseos = repositorioPaseos;
            this.repositorioHospedajes = repositorioHospedajes;
            this.unidadDeTrabajo = unidadDeTrabajo;
            this.pasarela = pasarela;
            this.proveedorTokens = proveedorTokens;
        }

        public String CrearCheckout(String claveReserva, Int64 idCliente, String urlDeRetorno, out String error)
        {
            error = null;

            Int64 idHospedaje;
            if (AyudanteReservas.EsClaveDeHospedaje(claveReserva, out idHospedaje))
            {
                return CrearCheckoutDeHospedaje(idHospedaje, claveReserva, idCliente, urlDeRetorno, out error);
            }

            var paseos = AyudanteReservas.Buscar(IncluirDatos(), claveReserva);
            if (paseos.Count == 0 || paseos[0].Pet.CustomerId != idCliente)
            {
                error = "La reserva no existe.";
                return null;
            }

            var activo = paseos.Where(x => x.Status != WalkStatus.Cancelled).ToList();
            if (activo.Count == 0 || activo.Any(x => x.Status != WalkStatus.Confirmed))
            {
                error = "Este paseo no está confirmado.";
                return null;
            }

            if (activo.Any(x => x.PaymentMethod != PaymentMethods.MercadoPago))
            {
                error = "Este paseo se paga en efectivo.";
                return null;
            }

            if (activo.Any(x => !x.FinishedAt.HasValue))
            {
                error = "El paseo se puede pagar cuando el paseador lo da por finalizado.";
                return null;
            }

            if (activo.Any(x => x.PaymentStatus != PaymentStatuses.Pending))
            {
                error = "Este paseo ya está pagado.";
                return null;
            }

            var primero = activo[0];
            var token = this.proveedorTokens.ObtenerTokenDeAcceso(primero.WalkerId, out error);
            if (token == null)
            {
                return null;
            }

            var nombresMascotas = String.Join(", ", activo.Select(x => x.Pet.Name));
            var nombrePaseador = primero.Walker != null ? primero.Walker.FirstName + " " + primero.Walker.LastName : "tu paseador";

            return this.pasarela.CrearCheckout(token, new SolicitudDeCheckout
            {
                Title = "Paseo de " + nombresMascotas + " con " + nombrePaseador,
                Amount = TotalDe(activo),
                ExternalReference = claveReserva,
                ReturnUrl = urlDeRetorno
            }, out error);
        }

        public String ConfirmarPago(String claveReserva, String idPago, out String error)
        {
            error = null;

            Int64 idHospedajeAConfirmar;
            if (AyudanteReservas.EsClaveDeHospedaje(claveReserva, out idHospedajeAConfirmar))
            {
                return ConfirmarPagoDeHospedaje(idHospedajeAConfirmar, claveReserva, idPago, out error);
            }

            var paseos = AyudanteReservas.Buscar(IncluirDatos(), claveReserva);
            if (paseos.Count == 0)
            {
                error = "La reserva no existe.";
                return null;
            }

            if (paseos.Any(x => x.PaymentMethod != PaymentMethods.MercadoPago))
            {
                error = "Este paseo se paga en efectivo.";
                return null;
            }

            //Ya estaba registrado (por ejemplo, el cliente vuelve dos veces a la pagina de retorno)
            var activo = paseos.Where(x => x.Status != WalkStatus.Cancelled).ToList();
            if (activo.Count > 0 && activo.All(x => x.PaymentStatus != PaymentStatuses.Pending))
            {
                return "approved";
            }

            if (activo.Count == 0 || activo.Any(x => !x.FinishedAt.HasValue))
            {
                error = "El paseo todavía no fue dado por finalizado.";
                return null;
            }

            var token = this.proveedorTokens.ObtenerTokenDeAcceso(activo[0].WalkerId, out error);
            if (token == null)
            {
                return null;
            }

            PagoPasarela pago = String.IsNullOrEmpty(idPago)
                ? this.pasarela.BuscarPagoAprobado(token, claveReserva, TotalDe(paseos), out error)
                : this.pasarela.ObtenerPago(token, idPago, out error);

            if (error != null)
            {
                return null;
            }
            if (pago == null)
            {
                return "pending";
            }

            //El pago tiene que ser de esta reserva y por el monto correcto: el id viaja por el navegador y no se confia en el
            if (!String.Equals(pago.ExternalReference, claveReserva, StringComparison.Ordinal))
            {
                error = "El pago no corresponde a esta reserva.";
                return null;
            }

            if (pago.Status != "approved")
            {
                return pago.Status == "rejected" || pago.Status == "cancelled" ? "failed" : "pending";
            }

            if (pago.Amount != TotalDe(paseos) || (pago.Currency != null && pago.Currency != "ARS"))
            {
                error = "El monto del pago no coincide con el de la reserva.";
                return null;
            }

            var idPagoPasarela = pago.Id;
            var idsPaseosReserva = paseos.Select(w => w.Id).ToList();
            var idPagoEnUso = this.repositorioPaseos.ObtenerTodos().
                                     Any(x => x.PaymentId == idPagoPasarela && !idsPaseosReserva.Contains(x.Id));
            if (idPagoEnUso)
            {
                error = "Ese pago ya se usó en otra reserva.";
                return null;
            }

            foreach (var paseo in activo)
            {
                paseo.PaymentId = pago.Id;
                paseo.PaymentStatus = PaymentStatuses.Paid;
                paseo.PaidAt = DateTime.Now;
            }
            this.unidadDeTrabajo.GuardarCambios();
            return "approved";
        }

        /* ---------- Hospedajes: el mismo circuito que un paseo, por el total del hospedaje ---------- */

        private String CrearCheckoutDeHospedaje(Int64 idHospedaje, String claveReserva, Int64 idCliente, String urlDeRetorno, out String error)
        {
            error = null;

            var hospedaje = this.repositorioHospedajes.TodosConIncluidos(x => x.Walker, x => x.Pets.Select(p => p.Pet)).FirstOrDefault(x => x.Id == idHospedaje);
            if (hospedaje == null || hospedaje.CustomerId != idCliente)
            {
                error = "El hospedaje no existe.";
                return null;
            }
            if (hospedaje.Status != WalkStatus.Confirmed)
            {
                error = "Este hospedaje no está confirmado.";
                return null;
            }
            if (hospedaje.PaymentMethod != PaymentMethods.MercadoPago)
            {
                error = "Este hospedaje se paga en efectivo.";
                return null;
            }
            if (!hospedaje.FinishedAt.HasValue)
            {
                error = "El hospedaje se puede pagar cuando el cuidador devolvió a los perros.";
                return null;
            }
            if (hospedaje.PaymentStatus != PaymentStatuses.Pending)
            {
                error = "Este hospedaje ya está pagado.";
                return null;
            }

            var token = this.proveedorTokens.ObtenerTokenDeAcceso(hospedaje.WalkerId, out error);
            if (token == null)
            {
                return null;
            }

            var nombres = String.Join(", ", hospedaje.Pets.Select(p => p.Pet.Name));
            var cuidador = hospedaje.Walker != null ? hospedaje.Walker.FirstName + " " + hospedaje.Walker.LastName : "tu cuidador";

            return this.pasarela.CrearCheckout(token, new SolicitudDeCheckout
            {
                Title = "Hospedaje de " + nombres + " con " + cuidador,
                Amount = hospedaje.Total,
                ExternalReference = claveReserva,
                ReturnUrl = urlDeRetorno
            }, out error);
        }

        private String ConfirmarPagoDeHospedaje(Int64 idHospedaje, String claveReserva, String idPago, out String error)
        {
            error = null;

            var hospedaje = this.repositorioHospedajes.ObtenerTodos().FirstOrDefault(x => x.Id == idHospedaje);
            if (hospedaje == null)
            {
                error = "El hospedaje no existe.";
                return null;
            }
            if (hospedaje.PaymentMethod != PaymentMethods.MercadoPago)
            {
                error = "Este hospedaje se paga en efectivo.";
                return null;
            }

            //Ya estaba registrado (por ejemplo, el cliente vuelve dos veces a la pagina de retorno)
            if (hospedaje.PaymentStatus != PaymentStatuses.Pending)
            {
                return "approved";
            }
            if (hospedaje.Status != WalkStatus.Confirmed || !hospedaje.FinishedAt.HasValue)
            {
                error = "El hospedaje todavía no terminó.";
                return null;
            }

            var token = this.proveedorTokens.ObtenerTokenDeAcceso(hospedaje.WalkerId, out error);
            if (token == null)
            {
                return null;
            }

            PagoPasarela pago = String.IsNullOrEmpty(idPago)
                ? this.pasarela.BuscarPagoAprobado(token, claveReserva, hospedaje.Total, out error)
                : this.pasarela.ObtenerPago(token, idPago, out error);

            if (error != null)
            {
                return null;
            }
            if (pago == null)
            {
                return "pending";
            }

            //El pago tiene que ser de este hospedaje y por el monto correcto: el id viaja por el navegador y no se confia en el
            if (!String.Equals(pago.ExternalReference, claveReserva, StringComparison.Ordinal))
            {
                error = "El pago no corresponde a este hospedaje.";
                return null;
            }
            if (pago.Status != "approved")
            {
                return pago.Status == "rejected" || pago.Status == "cancelled" ? "failed" : "pending";
            }
            if (pago.Amount != hospedaje.Total || (pago.Currency != null && pago.Currency != "ARS"))
            {
                error = "El monto del pago no coincide con el del hospedaje.";
                return null;
            }

            var idPagoPasarela = pago.Id;
            var idPagoEnUso = this.repositorioPaseos.ObtenerTodos().Any(x => x.PaymentId == idPagoPasarela) ||
                              this.repositorioHospedajes.ObtenerTodos().Any(x => x.PaymentId == idPagoPasarela && x.Id != hospedaje.Id);
            if (idPagoEnUso)
            {
                error = "Ese pago ya se usó en otra reserva.";
                return null;
            }

            hospedaje.PaymentId = pago.Id;
            hospedaje.PaymentStatus = PaymentStatuses.Paid;
            hospedaje.PaidAt = DateTime.Now;
            this.unidadDeTrabajo.GuardarCambios();
            return "approved";
        }

        private IQueryable<Walk> IncluirDatos()
        {
            return this.repositorioPaseos.TodosConIncluidos(x => x.Pet, x => x.Pet.Customer, x => x.Walker, x => x.Price);
        }

        //Monto de la reserva: un paseo por mascota
        private static Decimal TotalDe(IEnumerable<Walk> paseos)
        {
            return paseos.Sum(x => x.Price != null ? Convert.ToDecimal(x.Price.Amount) : 0m);
        }
    }
}
