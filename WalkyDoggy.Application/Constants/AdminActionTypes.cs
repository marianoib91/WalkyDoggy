using System;

namespace WalkyDoggy.Application.Constants
{
    //Tipos de accion que quedan en la bitacora del administrador
    public static class AdminActionTypes
    {
        public const String BlockUser = "BlockUser";

        public const String UnblockUser = "UnblockUser";

        public const String CreateAdmin = "CreateAdmin";

        public const String SaveBreed = "SaveBreed";

        public const String SetBreedActive = "SetBreedActive";

        public const String SaveSize = "SaveSize";

        public const String SetSizeActive = "SetSizeActive";

        public const String SaveTraitPair = "SaveTraitPair";

        public const String SetTraitPairActive = "SetTraitPairActive";

        public const String ReviewComplaint = "ReviewComplaint";

        public const String ResolveComplaint = "ResolveComplaint";

        public const String ViewComplaintChat = "ViewComplaintChat";

        public const String CancelWalk = "CancelWalk";

        public const String SaveAdvertiser = "SaveAdvertiser";

        public const String SetAdvertiserActive = "SetAdvertiserActive";

        public const String SaveAd = "SaveAd";

        public const String SetAdActive = "SetAdActive";

        public const String SetAdImage = "SetAdImage";

        public const String HideRating = "HideRating";

        public const String ShowRating = "ShowRating";

        public const String HidePetReview = "HidePetReview";

        public const String ShowPetReview = "ShowPetReview";
    }
}
