using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Entities
{
    public class Walk : IEntityBase
    {
        public Int64 Id { get; set; }

        public Int64 WalkerId { get; set; }

        public Int64 PetId { get; set; }

        public Int64 PriceId { get; set; }

        //En Date se guarda el dia del paseo
        public DateTime Date { get; set; }

        public String TimeFrom { get; set; }

        public String Details { get; set; }

        public Boolean Confirmed { get; set; }

        [NotMapped]
        public Boolean IsExpired { get; set; }

        public Walker Walker { get; set; }

        public Pet Pet { get; set; }

        public Price Price { get; set; }
    }
}
