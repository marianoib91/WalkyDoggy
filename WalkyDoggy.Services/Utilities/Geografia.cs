using System;
using System.Globalization;

namespace WalkyDoggy.Services.Utilities
{
    //Calculos con coordenadas: las latitudes y longitudes se guardan como texto con punto decimal (ej: "-32.95205")
    public static class Geografia
    {
        public static Boolean IntentarLeerCoordenadas(String latitud, String longitud, out Double latitudLeida, out Double longitudLeida)
        {
            latitudLeida = 0;
            longitudLeida = 0;

            return Double.TryParse(latitud, NumberStyles.Float, CultureInfo.InvariantCulture, out latitudLeida) &&
                   Double.TryParse(longitud, NumberStyles.Float, CultureInfo.InvariantCulture, out longitudLeida) &&
                   latitudLeida >= -90 && latitudLeida <= 90 && longitudLeida >= -180 && longitudLeida <= 180;
        }

        //Distancia en linea recta entre dos puntos (formula de Haversine)
        public static Double DistanciaEnKilometros(Double latitud1, Double longitud1, Double latitud2, Double longitud2)
        {
            const Double radioTierraKm = 6371;
            Func<Double, Double> aRadianes = grados => grados * Math.PI / 180;

            var deltaLatitud = aRadianes(latitud2 - latitud1);
            var deltaLongitud = aRadianes(longitud2 - longitud1);
            var a = Math.Sin(deltaLatitud / 2) * Math.Sin(deltaLatitud / 2) +
                    Math.Cos(aRadianes(latitud1)) * Math.Cos(aRadianes(latitud2)) *
                    Math.Sin(deltaLongitud / 2) * Math.Sin(deltaLongitud / 2);

            return radioTierraKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}
