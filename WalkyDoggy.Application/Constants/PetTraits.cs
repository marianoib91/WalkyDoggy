using System;
using System.Collections.Generic;
using System.Linq;

namespace WalkyDoggy.Application.Constants
{
    //Caracteristicas con las que el cliente describe a su perro. Vienen en pares opuestos y de cada par se elige una sola (o ninguna):
    //por ejemplo, un perro es Juguetón o Tranquilo, pero no las dos cosas. Se guardan como codigos separados por coma ("Playful,Runner").
    //Sirven para sugerir paseos en los que el paseador lleva perros parecidos (matching entre mascotas).
    public static class PetTraits
    {
        //Cuantas caracteristicas tienen que coincidir para considerar que dos perros son compatibles
        public const Int32 MinimoEnComun = 2;

        public static readonly String[][] Pares = new[]
        {
            new[] { "Playful", "Calm" },
            new[] { "Extroverted", "Introverted" },
            new[] { "Runner", "SunNapper" },
            new[] { "Obedient", "Naughty" },
            new[] { "Barker", "Quiet" },
            new[] { "WaterLover", "WaterAverse" },
            new[] { "Affectionate", "Independent" }
        };

        public static List<String> Todas()
        {
            return Pares.SelectMany(x => x).ToList();
        }

        //Los codigos de una lista guardada (vacia si no cargo ninguno)
        public static List<String> Leer(String guardadas)
        {
            if (String.IsNullOrWhiteSpace(guardadas))
            {
                return new List<String>();
            }

            return guardadas.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).
                             Select(x => x.Trim()).
                             Where(x => x.Length > 0).
                             Distinct().
                             ToList();
        }

        //Controla que los codigos existan y que no haya dos del mismo par. Devuelve el motivo del error, o null si esta bien.
        //En normalizadas queda la lista en el orden de los pares (o null si no hay ninguna).
        public static String Validar(String guardadas, out String normalizadas)
        {
            normalizadas = null;
            var elegidas = Leer(guardadas);
            var conocidas = Todas();

            if (elegidas.Any(x => !conocidas.Contains(x)))
            {
                return "Hay características de la mascota que no existen.";
            }
            if (Pares.Any(par => par.Count(x => elegidas.Contains(x)) > 1))
            {
                return "De cada par de características solo se puede elegir una (por ejemplo, Juguetón o Tranquilo).";
            }

            var ordenadas = conocidas.Where(x => elegidas.Contains(x)).ToList();
            normalizadas = ordenadas.Count == 0 ? null : String.Join(",", ordenadas);
            return null;
        }

        public static List<String> EnComun(List<String> unas, List<String> otras)
        {
            return unas.Where(x => otras.Contains(x)).ToList();
        }
    }
}
