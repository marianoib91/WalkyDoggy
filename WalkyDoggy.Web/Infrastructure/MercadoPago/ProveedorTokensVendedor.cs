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
    public class ProveedorTokensVendedor : IProveedorTokensVendedor
    {
        //Proposito con el que se cifran los tokens (tiene que ser el mismo al guardarlos y al leerlos)
        public const String TokenPurpose = "WalkyDoggy.MercadoPago.Token";

        //Se renueva con este margen de anticipacion
        private static readonly TimeSpan MargenRenovacion = TimeSpan.FromDays(2);

        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;

        public ProveedorTokensVendedor(IRepositorioEntidadBase<Walker> repositorioPaseadores, IUnidadDeTrabajo unidadDeTrabajo)
        {
            this.repositorioPaseadores = repositorioPaseadores;
            this.unidadDeTrabajo = unidadDeTrabajo;
        }

        public Boolean EstaVinculada(Int64 idPaseador)
        {
            var paseador = this.repositorioPaseadores.ObtenerUno(idPaseador);
            return paseador != null && !String.IsNullOrEmpty(paseador.MercadoPagoAccessToken);
        }

        public String ObtenerTokenDeAcceso(Int64 idPaseador, out String error)
        {
            error = null;

            var paseador = this.repositorioPaseadores.ObtenerUno(idPaseador);
            if (paseador == null || String.IsNullOrEmpty(paseador.MercadoPagoAccessToken))
            {
                error = "El paseador todavía no vinculó su cuenta de Mercado Pago.";
                return null;
            }

            var requiereRenovacion = paseador.MercadoPagoTokenExpiresAt.HasValue &&
                               paseador.MercadoPagoTokenExpiresAt.Value <= DateTime.UtcNow.Add(MargenRenovacion);
            if (requiereRenovacion && !String.IsNullOrEmpty(paseador.MercadoPagoRefreshToken))
            {
                try
                {
                    var configuracion = ConfiguracionMercadoPago.DesdeConfiguracion();
                    var tokenDeRenovacion = ProtectorDeSecretos.Desproteger(paseador.MercadoPagoRefreshToken, TokenPurpose);
                    if (configuracion.IsConfigured && tokenDeRenovacion != null)
                    {
                        var tokens = Task.Run(() => new ClienteOAuthMercadoPago(configuracion).RenovarAsync(tokenDeRenovacion)).GetAwaiter().GetResult();

                        paseador.MercadoPagoAccessToken = ProtectorDeSecretos.Proteger(tokens.AccessToken, TokenPurpose);
                        paseador.MercadoPagoRefreshToken = tokens.RefreshToken == null ? paseador.MercadoPagoRefreshToken : ProtectorDeSecretos.Proteger(tokens.RefreshToken, TokenPurpose);
                        paseador.MercadoPagoTokenExpiresAt = tokens.ExpiresAtUtc;
                        this.unidadDeTrabajo.GuardarCambios();
                    }
                }
                catch (Exception)
                {
                    //Si no se pudo renovar se sigue con el token actual: puede que todavia sirva
                }
            }

            var token = ProtectorDeSecretos.Desproteger(paseador.MercadoPagoAccessToken, TokenPurpose);
            if (token == null)
            {
                error = "No se pudo leer la cuenta de Mercado Pago del paseador. Que la vincule de nuevo desde su perfil.";
                return null;
            }

            return token;
        }
    }
}
