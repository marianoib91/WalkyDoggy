using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Entities
{
    public class City : IEntityBase
    {
        public Int64 Id { get; set; }

        public Int64 ProvinceId { get; set; }

        public String Name { get; set; }

        public String PostalCode { get; set; }

        public Province Province { get; set; }
    }
}
