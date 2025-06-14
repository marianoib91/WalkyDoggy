using WalkyDoggy.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Data.Configurations
{
    public class WalksConfiguration : EntityBaseConfiguration<Walk>
    {
        public WalksConfiguration()
        {
            Property(u => u.WalkerId).IsRequired();
            Property(u => u.PetId).IsRequired();
            Property(u => u.Date).IsRequired();
            Property(u => u.PriceId).IsRequired();
            Property(u => u.Details);
        }
    }
}
