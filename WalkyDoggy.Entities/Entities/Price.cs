using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Entities
{
    public class Price : IEntityBase
    {
        public Int64 Id { get; set; }

        public Double Amount { get; set; }
    }
}
