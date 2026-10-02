using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Services.Utilities
{
    public class ServicioDiaDeLaSemana
    {
        public String ObtenerDiaDeLaSemana(string diaDeLaSemana)
        {
            if (diaDeLaSemana == "Monday")
            {
                return "Lunes";
            }
            else if (diaDeLaSemana == "Tuesday")
            {
                return "Martes";
            }
            else if (diaDeLaSemana == "Wednesday")
            {
                return "Miércoles";

            }
            else if (diaDeLaSemana == "Thursday")
            {
                return "Jueves";
            }
            else if (diaDeLaSemana == "Friday")
            {
                return "Viernes";
            }
            else if (diaDeLaSemana == "Saturday")
            {
                return "Sábado";
            }
            else if (diaDeLaSemana == "Sunday")
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
