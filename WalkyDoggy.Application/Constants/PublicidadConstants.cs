using System;
using System.Collections.Generic;

namespace WalkyDoggy.Application.Constants
{
    //Rubros de los comercios que se anuncian
    public static class AdvertiserCategories
    {
        public const String PetShop = "PetShop";

        public const String Veterinary = "Veterinary";

        //Guardia veterinaria: atiende urgencias (por ejemplo, las 24 horas)
        public const String EmergencyVet = "EmergencyVet";

        public const String PetTransport = "PetTransport";

        public const String FeedStore = "FeedStore";

        public const String Groomer = "Groomer";

        public const String Trainer = "Trainer";

        public const String DayCare = "DayCare";

        public const String Other = "Other";

        public static readonly List<String> Todos = new List<String> { PetShop, Veterinary, EmergencyVet, PetTransport, FeedStore, Groomer, Trainer, DayCare, Other };
    }

    //A quien se le muestra un aviso
    public static class AdAudiences
    {
        public const String All = "All";

        public const String Customers = "Customers";

        public const String Walkers = "Walkers";

        public static readonly List<String> Todos = new List<String> { All, Customers, Walkers };
    }
}
