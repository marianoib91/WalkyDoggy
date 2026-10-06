using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities.Dtos;

namespace WalkyDoggy.Application.Dtos
{
    public class PetDto : IDto
    {
        public Int64 Id { get; set; }

        public Int64 CustomerId { get; set; }

        public Int64 BreedId { get; set; }

        public Int64 SizeId { get; set; }

        public String BreedName { get; set; }

        public String SizeName { get; set; }

        public String Name { get; set; }

        public String Description { get; set; }

        public Int64 Age { get; set; }

        public String ProfileImage { get; set; }

        //Caracteristicas del perro, codigos separados por coma (por ejemplo "Playful,Runner"); vacio si no marco ninguna
        public String Traits { get; set; }
    }
}
