using System;
using System.Collections.Generic;
using System.Linq;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Services.Utilities
{
    //Una reserva agrupa los paseos (uno por mascota) que salieron de una misma solicitud
    public static class BookingHelper
    {
        //Identifica la reserva: el BookingCode o, en paseos anteriores a las reservas agrupadas, "w" + Id del paseo
        public static String KeyOf(Walk walk)
        {
            return walk.BookingCode.HasValue ? walk.BookingCode.Value.ToString("N") : "w" + walk.Id;
        }

        public static List<Walk> Find(IQueryable<Walk> walks, String bookingKey)
        {
            if (!String.IsNullOrEmpty(bookingKey) && bookingKey.StartsWith("w"))
            {
                Int64 walkId;
                if (Int64.TryParse(bookingKey.Substring(1), out walkId))
                {
                    return walks.Where(x => x.Id == walkId).ToList();
                }
            }
            else
            {
                Guid bookingCode;
                if (Guid.TryParseExact(bookingKey ?? String.Empty, "N", out bookingCode))
                {
                    Guid? code = bookingCode;
                    return walks.Where(x => x.BookingCode == code).ToList();
                }
            }

            return new List<Walk>();
        }

        public static DateTime StartOf(Walk walk)
        {
            return walk.Date.Date.AddHours(Convert.ToInt32(walk.TimeFrom.Split(':')[0]));
        }

        //Los paseos duran una hora
        public static DateTime EndOf(Walk walk)
        {
            return StartOf(walk).AddHours(1);
        }
    }
}
