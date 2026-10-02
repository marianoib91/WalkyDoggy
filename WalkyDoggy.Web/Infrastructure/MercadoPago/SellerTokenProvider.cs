using System;
using System.Threading.Tasks;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Web.Infrastructure.Core;

namespace WalkyDoggy.Web.Infrastructure.MercadoPago
{
    //Entrega el access token de la cuenta de Mercado Pago que vinculo un paseador. Los tokens se guardan cifrados
    //(MachineKey) y, si estan por vencer, se renuevan con el refresh token antes de usarlos.
    public class SellerTokenProvider : ISellerTokenProvider
    {
        //Proposito con el que se cifran los tokens (tiene que ser el mismo al guardarlos y al leerlos)
        public const String TokenPurpose = "WalkyDoggy.MercadoPago.Token";

        //Se renueva con este margen de anticipacion
        private static readonly TimeSpan RenewalMargin = TimeSpan.FromDays(2);

        private readonly IEntityBaseRepository<Walker> walkersRepository;
        private readonly IUnitOfWork unitOfWork;

        public SellerTokenProvider(IEntityBaseRepository<Walker> walkersRepository, IUnitOfWork unitOfWork)
        {
            this.walkersRepository = walkersRepository;
            this.unitOfWork = unitOfWork;
        }

        public Boolean IsLinked(Int64 walkerId)
        {
            var walker = this.walkersRepository.GetSingle(walkerId);
            return walker != null && !String.IsNullOrEmpty(walker.MercadoPagoAccessToken);
        }

        public String GetAccessToken(Int64 walkerId, out String error)
        {
            error = null;

            var walker = this.walkersRepository.GetSingle(walkerId);
            if (walker == null || String.IsNullOrEmpty(walker.MercadoPagoAccessToken))
            {
                error = "El paseador todavía no vinculó su cuenta de Mercado Pago.";
                return null;
            }

            var needsRenewal = walker.MercadoPagoTokenExpiresAt.HasValue &&
                               walker.MercadoPagoTokenExpiresAt.Value <= DateTime.UtcNow.Add(RenewalMargin);
            if (needsRenewal && !String.IsNullOrEmpty(walker.MercadoPagoRefreshToken))
            {
                try
                {
                    var settings = MercadoPagoSettings.FromConfig();
                    var refreshToken = SecretProtector.Unprotect(walker.MercadoPagoRefreshToken, TokenPurpose);
                    if (settings.IsConfigured && refreshToken != null)
                    {
                        var tokens = Task.Run(() => new MercadoPagoOAuthClient(settings).RefreshAsync(refreshToken)).GetAwaiter().GetResult();

                        walker.MercadoPagoAccessToken = SecretProtector.Protect(tokens.AccessToken, TokenPurpose);
                        walker.MercadoPagoRefreshToken = tokens.RefreshToken == null ? walker.MercadoPagoRefreshToken : SecretProtector.Protect(tokens.RefreshToken, TokenPurpose);
                        walker.MercadoPagoTokenExpiresAt = tokens.ExpiresAtUtc;
                        this.unitOfWork.Commit();
                    }
                }
                catch (Exception)
                {
                    //Si no se pudo renovar se sigue con el token actual: puede que todavia sirva
                }
            }

            var token = SecretProtector.Unprotect(walker.MercadoPagoAccessToken, TokenPurpose);
            if (token == null)
            {
                error = "No se pudo leer la cuenta de Mercado Pago del paseador. Que la vincule de nuevo desde su perfil.";
                return null;
            }

            return token;
        }
    }
}
