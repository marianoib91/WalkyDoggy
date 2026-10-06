using System;
using System.Collections.Generic;
using System.IO;

namespace WalkyDoggy.Web.Infrastructure.Core
{
    //Controles comunes para las imagenes que suben las personas (denuncias, avisos de publicidad)
    public static class ArchivosDeImagen
    {
        public const Int32 TamanoMaximoBytes = 5 * 1024 * 1024;

        private static readonly Dictionary<String, String> TiposPermitidos = new Dictionary<String, String>
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".webp", "image/webp" }
        };

        //El tipo de contenido que corresponde a la extension, o null si no es una imagen permitida (jpg, png, gif o webp)
        public static String TipoDeContenido(String extension)
        {
            String tipo;
            return TiposPermitidos.TryGetValue((extension ?? String.Empty).ToLowerInvariant(), out tipo) ? tipo : null;
        }

        //No alcanza con la extension: se mira que los primeros bytes sean los de una imagen de ese formato
        public static Boolean EsImagenDeVerdad(String ruta, String extension)
        {
            var cabecera = new Byte[12];
            Int32 leidos;
            using (var flujo = File.OpenRead(ruta))
            {
                leidos = flujo.Read(cabecera, 0, cabecera.Length);
            }
            if (leidos < 12)
            {
                return false;
            }

            switch ((extension ?? String.Empty).ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                    return cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF;
                case ".png":
                    return cabecera[0] == 0x89 && cabecera[1] == 0x50 && cabecera[2] == 0x4E && cabecera[3] == 0x47;
                case ".gif":
                    return cabecera[0] == 0x47 && cabecera[1] == 0x49 && cabecera[2] == 0x46 && cabecera[3] == 0x38;
                case ".webp":
                    return cabecera[0] == 0x52 && cabecera[1] == 0x49 && cabecera[2] == 0x46 && cabecera[3] == 0x46 &&
                           cabecera[8] == 0x57 && cabecera[9] == 0x45 && cabecera[10] == 0x42 && cabecera[11] == 0x50;
                default:
                    return false;
            }
        }

        public static void EliminarSinErrores(String ruta)
        {
            try
            {
                if (!String.IsNullOrEmpty(ruta) && File.Exists(ruta))
                {
                    File.Delete(ruta);
                }
            }
            catch
            {
                //Un archivo que no se pudo borrar no tiene que tapar la respuesta al usuario
            }
        }
    }
}
