using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Services.Utilities
{
    public class DayOfWeekService
    {
        public String GetDayOfWeek(string dayOfWeek)
        {
            if (dayOfWeek == "Monday")
            {
                return "Lunes";
            }
            else if (dayOfWeek == "Tuesday")
            {
                return "Martes";
            }
            else if (dayOfWeek == "Wednesday")
            {
                return "Miércoles";

            }
            else if (dayOfWeek == "Thursday")
            {
                return "Jueves";
            }
            else if (dayOfWeek == "Friday")
            {
                return "Viernes";
            }
            else if (dayOfWeek == "Saturday")
            {
                return "Sábado";
            }
            else if (dayOfWeek == "Sunday")
            {
                return "Domingo";
            }
            else
            {
                return null;
            }
        }
    }
}
