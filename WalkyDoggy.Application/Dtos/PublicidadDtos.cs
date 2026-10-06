using System;
using System.Collections.Generic;

namespace WalkyDoggy.Application.Dtos
{
    /* ---------- Comercios (los ve el administrador) ---------- */

    public class AdvertiserDto
    {
        public Int64 Id { get; set; }

        public String Name { get; set; }

        public String Category { get; set; }

        public String Description { get; set; }

        public String Phone { get; set; }

        public String Website { get; set; }

        public String StreetName { get; set; }

        public Int64? StreetNumber { get; set; }

        public String CityName { get; set; }

        public String Latitude { get; set; }

        public String Longitude { get; set; }

        public Boolean Active { get; set; }

        //Cuantos avisos tiene en total y cuantos se estan mostrando hoy
        public Int32 AdCount { get; set; }

        public Int32 RunningAdCount { get; set; }
    }

    //Id = 0 crea un comercio nuevo; con Id edita sus datos
    public class SaveAdvertiserDto
    {
        public Int64 Id { get; set; }

        public String Name { get; set; }

        public String Category { get; set; }

        public String Description { get; set; }

        public String Phone { get; set; }

        public String Website { get; set; }

        public String StreetName { get; set; }

        public Int64? StreetNumber { get; set; }

        public String CityName { get; set; }

        public String Latitude { get; set; }

        public String Longitude { get; set; }
    }

    /* ---------- Avisos ---------- */

    public class AdminAdDto
    {
        public Int64 Id { get; set; }

        public Int64 AdvertiserId { get; set; }

        public String AdvertiserName { get; set; }

        public String AdvertiserCategory { get; set; }

        public String Title { get; set; }

        public String Text { get; set; }

        public String ImageUrl { get; set; }

        public String LinkUrl { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        //All | Customers | Walkers
        public String Audience { get; set; }

        public Int32? RadiusKm { get; set; }

        public Boolean Active { get; set; }

        //Running (se muestra hoy) | Scheduled (todavia no empezo) | Finished (ya termino) | Paused (pausado, o su comercio esta de baja)
        public String Status { get; set; }

        public Int32 Impressions { get; set; }

        public Int32 Clicks { get; set; }

        //Clics sobre vistas, en porcentaje (0 si todavia no se mostro)
        public Double ClickRate { get; set; }
    }

    //Id = 0 crea un aviso nuevo; con Id edita el contenido y las condiciones (las estadisticas se conservan)
    public class SaveAdDto
    {
        public Int64 Id { get; set; }

        public Int64 AdvertiserId { get; set; }

        public String Title { get; set; }

        public String Text { get; set; }

        public String LinkUrl { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public String Audience { get; set; }

        //null o 0 = se muestra a todos
        public Int32? RadiusKm { get; set; }
    }

    /* ---------- Lo que ven los clientes y los paseadores ---------- */

    public class PublicAdDto
    {
        public Int64 Id { get; set; }

        public String Title { get; set; }

        public String Text { get; set; }

        public String ImageUrl { get; set; }

        public String LinkUrl { get; set; }

        public String AdvertiserName { get; set; }

        public String Category { get; set; }

        //Direccion del comercio, para mostrar (ej: "Mitre 1234, Rosario")
        public String Address { get; set; }

        public String Phone { get; set; }

        //A cuantos km esta el comercio de quien mira (null si no se sabe donde esta)
        public Double? DistanceKm { get; set; }
    }

    //Un comercio en el directorio "Comercios amigos"
    public class PublicAdvertiserDto
    {
        public Int64 Id { get; set; }

        public String Name { get; set; }

        public String Category { get; set; }

        public String Description { get; set; }

        public String Phone { get; set; }

        public String Website { get; set; }

        public String Address { get; set; }

        //Para el enlace "Como llegar" (null si el comercio no esta ubicado en el mapa)
        public String Latitude { get; set; }

        public String Longitude { get; set; }

        //A cuantos km esta de quien mira (null si no se sabe donde esta o el comercio no esta ubicado)
        public Double? DistanceKm { get; set; }

        //Promociones que tiene hoy para quien mira
        public List<PublicPromotionDto> Promotions { get; set; }
    }

    public class PublicPromotionDto
    {
        public Int64 AdId { get; set; }

        public String Title { get; set; }

        public String Text { get; set; }

        public String LinkUrl { get; set; }
    }

    public class AdImpressionsDto
    {
        public List<Int64> Ids { get; set; }
    }

    public class AdClickDto
    {
        public Int64 Id { get; set; }
    }
}
