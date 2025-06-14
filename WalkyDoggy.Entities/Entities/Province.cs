using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Entities
{
    public class Province : IEntityBase
    {
        public Int64 Id { get; set; }

        public String Name { get; set; }
    }
}
