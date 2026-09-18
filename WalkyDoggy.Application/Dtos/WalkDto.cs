using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities.Dtos;

namespace WalkyDoggy.Application.Dtos
{
    public class WalkDto : IDto
    {
        public Int64 Id { get; set; }

        public DateTime Date { get; set; }

        public String Details { get; set; }
        
        public String PetName { get; set; }

        public String PetProfileImage { get; set; }

        public String CustomerFullName { get; set; }

        public String Location { get; set; }

        //Coordenadas del lugar de retiro (para el "Como llegar" del paseador)
        public String Latitude { get; set; }

        public String Longitude { get; set; }

        public String TimeFrom { get; set; }

        public Boolean Confirmed { get; set; }

        public Boolean IsExpired { get; set; }
    }
}
