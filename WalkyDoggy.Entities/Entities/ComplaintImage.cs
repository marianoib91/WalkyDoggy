using System;

namespace WalkyDoggy.Entities
{
    //Imagen adjunta a una denuncia (captura de pantalla, foto de la mascota, etc.). El archivo esta en App_Data/complaints, fuera de las carpetas publicas.
    public class ComplaintImage : IEntityBase
    {
        public Int64 Id { get; set; }

        public Int64 ComplaintId { get; set; }

        //Nombre con el que se guardo el archivo (un identificador al azar mas la extension)
        public String FileName { get; set; }

        //Nombre que tenia el archivo en la computadora de quien lo subio
        public String OriginalName { get; set; }

        public String ContentType { get; set; }

        public Int32 SizeBytes { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
