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
    public class PaymentAppService : IPaymentAppService
    {
        private readonly IEntityBaseRepository<Walk> walksRepository;
        private readonly IUnitOfWork unitOfWork;
        private readonly IPaymentGateway gateway;
        private readonly ISellerTokenProvider sellerTokens;

        public PaymentAppService(IEntityBaseRepository<Walk> walksRepository,
                                 IUnitOfWork unitOfWork,
                                 IPaymentGateway gateway,
                                 ISellerTokenProvider sellerTokens)
        {
            this.walksRepository = walksRepository;
            this.unitOfWork = unitOfWork;
            this.gateway = gateway;
            this.sellerTokens = sellerTokens;
        }

        public String CreateCheckout(String bookingKey, Int64 customerId, String returnUrl, out String error)
        {
            error = null;

            var walks = BookingHelper.Find(IncludeData(), bookingKey);
            if (walks.Count == 0 || walks[0].Pet.CustomerId != customerId)
            {
                error = "La reserva no existe.";
                return null;
            }

            var active = walks.Where(x => x.Status != WalkStatus.Cancelled).ToList();
            if (active.Count == 0 || active.Any(x => x.Status != WalkStatus.Confirmed))
            {
                error = "Este paseo no está confirmado.";
                return null;
            }

            if (active.Any(x => x.PaymentMethod != PaymentMethods.MercadoPago))
            {
                error = "Este paseo se paga en efectivo.";
                return null;
            }

            if (active.Any(x => !x.FinishedAt.HasValue))
            {
                error = "El paseo se puede pagar cuando el paseador lo da por finalizado.";
                return null;
            }

            if (active.Any(x => x.PaymentStatus != PaymentStatuses.Pending))
            {
                error = "Este paseo ya está pagado.";
                return null;
            }

            var first = active[0];
            var token = this.sellerTokens.GetAccessToken(first.WalkerId, out error);
            if (token == null)
            {
                return null;
            }

            var petNames = String.Join(", ", active.Select(x => x.Pet.Name));
            var walkerName = first.Walker != null ? first.Walker.FirstName + " " + first.Walker.LastName : "tu paseador";

            return this.gateway.CreateCheckout(token, new CheckoutRequest
            {
                Title = "Paseo de " + petNames + " con " + walkerName,
                Amount = TotalOf(active),
                ExternalReference = bookingKey,
                ReturnUrl = returnUrl
            }, out error);
        }

        public String ConfirmPayment(String bookingKey, String paymentId, out String error)
        {
            error = null;

            var walks = BookingHelper.Find(IncludeData(), bookingKey);
            if (walks.Count == 0)
            {
                error = "La reserva no existe.";
                return null;
            }

            if (walks.Any(x => x.PaymentMethod != PaymentMethods.MercadoPago))
            {
                error = "Este paseo se paga en efectivo.";
                return null;
            }

            //Ya estaba registrado (por ejemplo, el cliente vuelve dos veces a la pagina de retorno)
            var active = walks.Where(x => x.Status != WalkStatus.Cancelled).ToList();
            if (active.Count > 0 && active.All(x => x.PaymentStatus != PaymentStatuses.Pending))
            {
                return "approved";
            }

            if (active.Count == 0 || active.Any(x => !x.FinishedAt.HasValue))
            {
                error = "El paseo todavía no fue dado por finalizado.";
                return null;
            }

            var token = this.sellerTokens.GetAccessToken(active[0].WalkerId, out error);
            if (token == null)
            {
                return null;
            }

            GatewayPayment payment = String.IsNullOrEmpty(paymentId)
                ? this.gateway.FindApprovedPayment(token, bookingKey, TotalOf(walks), out error)
                : this.gateway.GetPayment(token, paymentId, out error);

            if (error != null)
            {
                return null;
            }
            if (payment == null)
            {
                return "pending";
            }

            //El pago tiene que ser de esta reserva y por el monto correcto: el id viaja por el navegador y no se confia en el
            if (!String.Equals(payment.ExternalReference, bookingKey, StringComparison.Ordinal))
            {
                error = "El pago no corresponde a esta reserva.";
                return null;
            }

            if (payment.Status != "approved")
            {
                return payment.Status == "rejected" || payment.Status == "cancelled" ? "failed" : "pending";
            }

            if (payment.Amount != TotalOf(walks) || (payment.Currency != null && payment.Currency != "ARS"))
            {
                error = "El monto del pago no coincide con el de la reserva.";
                return null;
            }

            var gatewayPaymentId = payment.Id;
            var bookingWalkIds = walks.Select(w => w.Id).ToList();
            var paymentIdInUse = this.walksRepository.GetAll().
                                     Any(x => x.PaymentId == gatewayPaymentId && !bookingWalkIds.Contains(x.Id));
            if (paymentIdInUse)
            {
                error = "Ese pago ya se usó en otra reserva.";
                return null;
            }

            foreach (var walk in active)
            {
                walk.PaymentId = payment.Id;
                walk.PaymentStatus = PaymentStatuses.Paid;
                walk.PaidAt = DateTime.Now;
            }
            this.unitOfWork.Commit();
            return "approved";
        }

        private IQueryable<Walk> IncludeData()
        {
            return this.walksRepository.AllIncluding(x => x.Pet, x => x.Pet.Customer, x => x.Walker, x => x.Price);
        }

        //Monto de la reserva: un paseo por mascota
        private static Decimal TotalOf(IEnumerable<Walk> walks)
        {
            return walks.Sum(x => x.Price != null ? Convert.ToDecimal(x.Price.Amount) : 0m);
        }
    }
}
