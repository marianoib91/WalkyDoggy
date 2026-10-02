using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Entities
{
    public class Walker : IEntityBase
    {
        public Int64 Id { get; set; }

        public Int64 UserId { get; set; }

        public Int64 CityId { get; set; }

        public Int64 PriceId { get; set; }

        public String FirstName { get; set; }

        public String LastName { get; set; }

        public String Email { get; set; }

        public String Description { get; set; }

        public String Phone { get; set; }

        public String StreetName { get; set; }

        public Int64 StreetNumber { get; set; }

        public String ProfileImage { get; set; }

        //Direccion de referencia donde el paseador quiere trabajar (por ejemplo, una plaza) y sus coordenadas (decimales con punto, ej: -32.95205)
        public String Latitude { get; set; }

        public String Longitude { get; set; }

        //Radio, en kilometros, alrededor de la direccion de referencia en el que el paseador acepta retirar mascotas
        public Double ServiceRadiusKm { get; set; }

        //Alias o CBU/CVU donde el paseador recibe transferencias (opcional)
        public String PayoutAccount { get; set; }

        //Cuenta de Mercado Pago vinculada por el paseador (OAuth). Los tokens se guardan cifrados y nunca salen del servidor.
        public String MercadoPagoUserId { get; set; }

        public String MercadoPagoAccessToken { get; set; }

        public String MercadoPagoRefreshToken { get; set; }

        public String MercadoPagoPublicKey { get; set; }

        public DateTime? MercadoPagoTokenExpiresAt { get; set; }

        public DateTime? MercadoPagoLinkedAt { get; set; }

        public User User { get; set; }

        public City City { get; set; }

        public Price Price { get; set; }
    }
}
