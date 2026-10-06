using System;
using System.Collections.Generic;

namespace WalkyDoggy.Application.Constants
{
    //Por que se denuncia (la lista que se ofrece al denunciante)
    public static class ComplaintReasons
    {
        public const String AnimalAbuse = "AnimalAbuse";

        public const String DisrespectfulTreatment = "DisrespectfulTreatment";

        public const String NoShow = "NoShow";

        public const String UndueCharge = "UndueCharge";

        public const String Other = "Other";

        public static readonly List<String> Todos = new List<String> { AnimalAbuse, DisrespectfulTreatment, NoShow, UndueCharge, Other };
    }

    //Open = recien llegada, InReview = un administrador la esta mirando, Resolved = cerrada con una resolucion
    public static class ComplaintStatus
    {
        public const String Open = "Open";

        public const String InReview = "InReview";

        public const String Resolved = "Resolved";
    }

    //Como se cierra una denuncia. Warning y Block son sanciones: la persona denunciada se entera, con el motivo.
    public static class ComplaintResolution
    {
        public const String NoAction = "NoAction";

        public const String Warning = "Warning";

        public const String Block = "Block";
    }
}
