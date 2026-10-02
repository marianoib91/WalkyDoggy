using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Services.Utilities;

namespace WalkyDoggy.Services.Services
{
    public class PaymentAppService : IPaymentAppService
    {
        //Si el cliente no reclama dentro de este plazo desde que termino el paseo, el pago se libera solo
        private static readonly TimeSpan ReleaseDelay = TimeSpan.FromHours(24);

        private readonly IEntityBaseRepository<Walk> walksRepository;
        private readonly IUnitOfWork unitOfWork;
        private readonly IPaymentGateway gateway;
        private readonly INotificationAppService notificationAppService;
        private readonly IEntityBaseRepository<Walker> walkersRepository;

        public PaymentAppService(IEntityBaseRepository<Walk> walksRepository,
                                 IEntityBaseRepository<Walker> walkersRepository,
                                 IUnitOfWork unitOfWork,
                                 IPaymentGateway gateway,
                                 INotificationAppService notificationAppService)
        {
            this.walksRepository = walksRepository;
            this.walkersRepository = walkersRepository;
            this.unitOfWork = unitOfWork;
            this.gateway = gateway;
            this.notificationAppService = notificationAppService;
        }

        public String CreateCheckout(String bookingKey, Int64 customerId, String returnUrl, out String error)
        {
            var walks = FindOwn(bookingKey, customerId, out error);
            if (walks == null)
            {
                return null;
            }

            if (!gateway.IsConfigured)
            {
                error = "Los pagos con Mercado Pago todavía no están configurados en el sistema.";
                return null;
            }

            var active = walks.Where(x => x.Status != WalkStatus.Cancelled).ToList();
            if (active.Count == 0 || active.Any(x => x.Status != WalkStatus.Confirmed))
            {
                error = "El paseo se puede pagar cuando el paseador lo confirma.";
                return null;
            }

            if (active.Any(x => x.PaymentMethod != PaymentMethods.MercadoPago))
            {
                error = "Este paseo se paga en efectivo.";
                return null;
            }

            if (active.Any(x => x.PaymentStatus != PaymentStatuses.Pending))
            {
                error = "Este paseo ya está pagado.";
                return null;
            }

            if (BookingHelper.StartOf(active[0]) <= DateTime.Now)
            {
                error = "El paseo ya comenzó y no se puede pagar.";
                return null;
            }

            var first = active[0];
            var petNames = String.Join(", ", active.Select(x => x.Pet.Name));
            var walkerName = first.Walker != null ? first.Walker.FirstName + " " + first.Walker.LastName : "tu paseador";

            return gateway.CreateCheckout(new CheckoutRequest
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

            GatewayPayment payment = String.IsNullOrEmpty(paymentId)
                ? gateway.FindApprovedPayment(bookingKey, out error)
                : gateway.GetPayment(paymentId, out error);

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

            //El cliente pago pero la reserva se cancelo mientras tanto: se le devuelve el dinero
            if (active.Count == 0)
            {
                if (!gateway.Refund(payment.Id, out error))
                {
                    return null;
                }

                foreach (var walk in walks)
                {
                    walk.PaymentId = payment.Id;
                    walk.PaymentStatus = PaymentStatuses.Refunded;
                    walk.PaidAt = DateTime.Now;
                    walk.RefundedAt = DateTime.Now;
                }
                this.unitOfWork.Commit();
                return "refunded";
            }

            foreach (var walk in active)
            {
                walk.PaymentId = payment.Id;
                walk.PaymentStatus = PaymentStatuses.Held;
                walk.PaidAt = DateTime.Now;
            }
            this.unitOfWork.Commit();

            this.notificationAppService.PaymentHeld(active);
            return "approved";
        }

        public Boolean Release(String bookingKey, Int64 customerId, out String error)
        {
            var walks = FindOwn(bookingKey, customerId, out error);
            if (walks == null)
            {
                return false;
            }

            var held = walks.Where(x => x.Status == WalkStatus.Confirmed && x.PaymentStatus == PaymentStatuses.Held).ToList();
            if (held.Count == 0)
            {
                error = "Este pago no está retenido.";
                return false;
            }

            if (BookingHelper.EndOf(held[0]) > DateTime.Now)
            {
                error = "Todavía no terminó el paseo.";
                return false;
            }

            held.ForEach(x =>
            {
                x.PaymentStatus = PaymentStatuses.Released;
                x.ReleasedAt = DateTime.Now;
            });
            this.unitOfWork.Commit();

            this.notificationAppService.PaymentReleased(held);
            return true;
        }

        public Boolean Dispute(String bookingKey, Int64 customerId, String reason, out String error)
        {
            var walks = FindOwn(bookingKey, customerId, out error);
            if (walks == null)
            {
                return false;
            }

            reason = (reason ?? String.Empty).Trim();
            if (reason.Length == 0 || reason.Length > 500)
            {
                error = "Contanos qué pasó con el paseo (hasta 500 caracteres).";
                return false;
            }

            var held = walks.Where(x => x.Status == WalkStatus.Confirmed && x.PaymentStatus == PaymentStatuses.Held).ToList();
            if (held.Count == 0)
            {
                error = "Solo se puede reclamar un paseo con el pago retenido.";
                return false;
            }

            if (BookingHelper.StartOf(held[0]) > DateTime.Now)
            {
                error = "El paseo todavía no comenzó. Si ya no lo necesitás, cancelalo y se te devuelve el pago.";
                return false;
            }

            held.ForEach(x =>
            {
                x.PaymentStatus = PaymentStatuses.Disputed;
                x.PaymentDisputeReason = reason;
            });
            this.unitOfWork.Commit();

            this.notificationAppService.PaymentDisputed(held);
            return true;
        }

        public Boolean Refund(List<Walk> walks, out String error)
        {
            error = null;

            var held = walks.Where(x => x.PaymentStatus == PaymentStatuses.Held).ToList();
            if (held.Count == 0)
            {
                return true;
            }

            var paymentId = held[0].PaymentId;
            if (String.IsNullOrEmpty(paymentId))
            {
                error = "No se encontró el pago de la reserva en Mercado Pago.";
                return false;
            }

            if (!gateway.Refund(paymentId, out error))
            {
                return false;
            }

            walks.Where(x => x.PaymentStatus == PaymentStatuses.Held).ToList().ForEach(x =>
            {
                x.PaymentStatus = PaymentStatuses.Refunded;
                x.RefundedAt = DateTime.Now;
            });
            return true;
        }

        public void ReleaseDuePayments()
        {
            var now = DateTime.Now;
            var held = this.walksRepository.GetAll().
                            Where(x => x.PaymentStatus == PaymentStatuses.Held && x.Status == WalkStatus.Confirmed && x.Date <= now).
                            ToList();

            var due = held.Where(x => BookingHelper.EndOf(x) + ReleaseDelay <= now).ToList();
            if (due.Count == 0)
            {
                return;
            }

            due.ForEach(x =>
            {
                x.PaymentStatus = PaymentStatuses.Released;
                x.ReleasedAt = now;
            });
            this.unitOfWork.Commit();

            this.notificationAppService.PaymentReleased(due);
        }

        public WalkerBalanceDto GetWalkerBalance(Int64 walkerId)
        {
            ReleaseDuePayments();

            var walks = this.walksRepository.AllIncluding(x => x.Price).
                             Where(x => x.WalkerId == walkerId && x.PaymentMethod == PaymentMethods.MercadoPago && x.Status == WalkStatus.Confirmed).
                             ToList();

            Func<String, Double> sumOf = status => walks.Where(x => x.PaymentStatus == status).Sum(x => x.Price != null ? x.Price.Amount : 0);

            var walker = this.walkersRepository.GetSingle(walkerId);

            return new WalkerBalanceDto
            {
                Retained = sumOf(PaymentStatuses.Held),
                ToSettle = sumOf(PaymentStatuses.Released),
                InReview = sumOf(PaymentStatuses.Disputed),
                PayoutAccount = walker != null ? walker.PayoutAccount : null,
                PayoutHolder = walker != null ? walker.PayoutHolder : null,
                HasPayoutAccount = walker != null && !String.IsNullOrEmpty(walker.PayoutAccount)
            };
        }

        public PayoutAccountDto GetPayoutAccount(Int64 walkerId)
        {
            var walker = this.walkersRepository.GetSingle(walkerId);

            return new PayoutAccountDto
            {
                Account = walker != null ? walker.PayoutAccount : null,
                Holder = walker != null ? walker.PayoutHolder : null
            };
        }

        public Boolean SavePayoutAccount(Int64 walkerId, String account, String holder, out String error)
        {
            error = null;

            var walker = this.walkersRepository.GetSingle(walkerId);
            if (walker == null)
            {
                error = "El paseador no existe.";
                return false;
            }

            var normalized = NormalizePayoutAccount(account);
            if (normalized == null)
            {
                error = "Ingresá un alias (de 6 a 20 letras, números, puntos o guiones) o un CBU/CVU de 22 dígitos.";
                return false;
            }

            holder = (holder ?? String.Empty).Trim();
            if (holder.Length == 0 || holder.Length > 100)
            {
                error = "Ingresá el nombre del titular de la cuenta (hasta 100 caracteres).";
                return false;
            }

            walker.PayoutAccount = normalized;
            walker.PayoutHolder = holder;
            this.unitOfWork.Commit();
            return true;
        }

        //Un CBU/CVU son 22 digitos (se aceptan espacios o guiones al tipearlo); un alias tiene de 6 a 20 caracteres
        //entre letras, numeros, puntos y guiones. Devuelve null si no es ninguno de los dos.
        private static String NormalizePayoutAccount(String account)
        {
            var text = (account ?? String.Empty).Trim();

            var digits = text.Replace(" ", String.Empty).Replace("-", String.Empty);
            if (digits.Length > 0 && digits.All(Char.IsDigit))
            {
                return digits.Length == 22 ? digits : null;
            }

            return Regex.IsMatch(text, "^[A-Za-z0-9.-]{6,20}$") ? text : null;
        }

        private IQueryable<Walk> IncludeData()
        {
            return this.walksRepository.AllIncluding(x => x.Pet, x => x.Pet.Customer, x => x.Walker, x => x.Price);
        }

        //Los paseos de la reserva, siempre que pertenezcan al cliente
        private List<Walk> FindOwn(String bookingKey, Int64 customerId, out String error)
        {
            error = null;

            var walks = BookingHelper.Find(IncludeData(), bookingKey);
            if (walks.Count == 0 || walks[0].Pet.CustomerId != customerId)
            {
                error = "La reserva no existe.";
                return null;
            }

            return walks;
        }

        //Monto de la reserva: un paseo por mascota. Las reservas se cancelan enteras, por eso se suman todos los paseos.
        private static Decimal TotalOf(IEnumerable<Walk> walks)
        {
            return walks.Sum(x => x.Price != null ? Convert.ToDecimal(x.Price.Amount) : 0m);
        }
    }
}
