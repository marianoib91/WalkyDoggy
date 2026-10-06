using System;

namespace WalkyDoggy.Entities
{
    //Un comercio que se anuncia en la app: pet shop, veterinaria, empresa de transporte de mascotas, forrajeria, etc.
    //Lo carga y administra el administrador. Se da de baja (Active = false), no se borra: sus avisos pasados quedan con sus estadisticas.
    public class Advertiser : IEntityBase
    {
        public Int64 Id { get; set; }

        public String Name { get; set; }

        //Ver AdvertiserCategories
        public String Category { get; set; }

        public String Description { get; set; }

        public String Phone { get; set; }

        //Pagina web o red social (http o https)
        public String Website { get; set; }

        public String StreetName { get; set; }

        public Int64? StreetNumber { get; set; }

        public String CityName { get; set; }

        //Coordenadas del local (decimales con punto): se usan para mostrar el aviso solo a quienes estan cerca
        public String Latitude { get; set; }

        public String Longitude { get; set; }

        public Boolean Active { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
