using System;
using System.Collections.Generic;
using System.Linq;

namespace WalkyDoggy.Application.Constants
{
    //Caracteristicas con las que el cliente describe a su perro. Vienen en pares opuestos y de cada par se elige una sola (o ninguna):
    //por ejemplo, un perro es Juguetón o Tranquilo, pero no las dos cosas. Se guardan como codigos separados por coma ("Playful,Runner").
    //Sirven para sugerir paseos en los que el paseador lleva perros parecidos (matching entre mascotas).
    //El catalogo de caracteristicas (los pares y sus textos) lo administra el administrador y esta en la base de datos (ver IServicioCaracteristicas);
    //aca quedan solo las reglas del matching y la lectura de la lista guardada.
    public static class PetTraits
    {
        //Cuantas caracteristicas tienen que coincidir para considerar que dos perros son compatibles
        public const Int32 MinimoEnComun = 2;

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

        public static List<String> EnComun(List<String> unas, List<String> otras)
        {
            return unas.Where(x => otras.Contains(x)).ToList();
        }
    }
}
