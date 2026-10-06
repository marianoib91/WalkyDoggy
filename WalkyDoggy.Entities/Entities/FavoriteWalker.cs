using System;

namespace WalkyDoggy.Entities
{
    //Un paseador que el cliente marco como favorito, para encontrarlo rapido al pedir un paseo
    public class FavoriteWalker : IEntityBase
    {
        public Int64 Id { get; set; }

        public Int64 CustomerId { get; set; }

        public Int64 WalkerId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
