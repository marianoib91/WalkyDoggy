using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{

    public class PricesConfiguration : EntityBaseConfiguration<Price>
    {
        public PricesConfiguration()
        {
            Property(u => u.Amount).IsRequired();
        }
    }
}
