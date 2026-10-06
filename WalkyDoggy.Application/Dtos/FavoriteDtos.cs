using System;

namespace WalkyDoggy.Application.Dtos
{
    //Marcar o desmarcar a un paseador como favorito de un cliente
    public class SetFavoriteDto
    {
        public Int64 CustomerId { get; set; }

        public Int64 WalkerId { get; set; }

        public Boolean Favorite { get; set; }
    }
}
