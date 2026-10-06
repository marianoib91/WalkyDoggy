using System;

namespace WalkyDoggy.Entities
{
    //Un aviso publicitario de un comercio. Se muestra en las pantallas de inicio mientras este activo y dentro de sus fechas,
    //a quienes corresponda segun el destinatario y, si tiene alcance, a quienes esten dentro de ese radio del comercio.
    public class Ad : IEntityBase
    {
        public Int64 Id { get; set; }

        public Int64 AdvertiserId { get; set; }

        public String Title { get; set; }

        public String Text { get; set; }

        //Nombre del archivo de la imagen (en Content/images/uploads/ads); null si no tiene
        public String ImageFile { get; set; }

        //Adonde lleva el aviso al tocarlo (http o https); null si no lleva a ningun lado
        public String LinkUrl { get; set; }

        //Primer y ultimo dia (inclusive) en que se muestra
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        //All | Customers | Walkers (ver AdAudiences)
        public String Audience { get; set; }

        //Alcance en km alrededor del comercio; null = se muestra a todos
        public Int32? RadiusKm { get; set; }

        public Boolean Active { get; set; }

        //Cuantas veces se mostro y cuantas veces lo tocaron
        public Int32 Impressions { get; set; }

        public Int32 Clicks { get; set; }

        public DateTime CreatedAt { get; set; }

        public Advertiser Advertiser { get; set; }
    }
}
