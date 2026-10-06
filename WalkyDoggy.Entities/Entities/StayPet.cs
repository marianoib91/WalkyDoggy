using System;

namespace WalkyDoggy.Entities
{
    //Un perro que se queda en un hospedaje
    public class StayPet : IEntityBase
    {
        public Int64 Id { get; set; }

        public Int64 StayId { get; set; }

        public Int64 PetId { get; set; }

        public Pet Pet { get; set; }
    }
}
