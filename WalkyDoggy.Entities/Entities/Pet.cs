using WalkyDoggy.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities.Entities;

namespace WalkyDoggy.Entities
{
    public class Pet : IEntityBase
    {
        public Int64 Id { get; set; }

        public Int64 CustomerId { get; set; }

        public Int64 SizeId { get; set; }

        public Int64 BreedId { get; set; }

        public String Name { get; set; }

        public Int64 Age { get; set; }

        public String Description { get; set; }

        public String ProfileImage { get; set; }

        //Caracteristicas del perro (codigos separados por coma, ver PetTraits); null si todavia no marco ninguna
        public String Traits { get; set; }

        public Customer Customer { get; set; }

        public Size Size { get; set; }

        public Breed Breed { get; set; }

    }
}
