using System;
using System.Collections.Generic;
using System.Linq;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Services.Utilities
{
    //Una reserva agrupa los paseos (uno por mascota) que salieron de una misma solicitud
    public static class AyudanteReservas
    {
        //Identifica la reserva: el BookingCode o, en paseos anteriores a las reservas agrupadas, "w" + Id del paseo
        public static String ClaveDe(Walk paseo)
        {
            return paseo.BookingCode.HasValue ? paseo.BookingCode.Value.ToString("N") : "w" + paseo.Id;
        }

        //La clave de un hospedaje es "h" + id (las de los paseos son "w" + id o un codigo hexadecimal, asi que no se pisan)
        public static Boolean EsClaveDeHospedaje(String claveReserva, out Int64 idHospedaje)
        {
            idHospedaje = 0;
            return !String.IsNullOrEmpty(claveReserva) && claveReserva.StartsWith("h") && Int64.TryParse(claveReserva.Substring(1), out idHospedaje);
        }

        public static List<Walk> Buscar(IQueryable<Walk> paseos, String claveReserva)
        {
            if (!String.IsNullOrEmpty(claveReserva) && claveReserva.StartsWith("w"))
            {
                Int64 idPaseo;
                if (Int64.TryParse(claveReserva.Substring(1), out idPaseo))
                {
                    return paseos.Where(x => x.Id == idPaseo).ToList();
                }
            }
            else
            {
                Guid codigoReserva;
                if (Guid.TryParseExact(claveReserva ?? String.Empty, "N", out codigoReserva))
                {
                    Guid? codigo = codigoReserva;
                    return paseos.Where(x => x.BookingCode == codigo).ToList();
                }
            }

            return new List<Walk>();
        }

        public static DateTime InicioDe(Walk paseo)
        {
            return paseo.Date.Date.AddHours(Convert.ToInt32(paseo.TimeFrom.Split(':')[0]));
        }

        //Los paseos duran una hora
        public static DateTime FinDe(Walk paseo)
        {
            return InicioDe(paseo).AddHours(1);
        }
    }
}
