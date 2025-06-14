using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class WorkDaysConfiguration : EntityBaseConfiguration<WorkDay>
    {
        public WorkDaysConfiguration()
        {

            Property(u => u.DayOfWeek).IsRequired().HasMaxLength(50);
            Property(u => u.TimeFrom).IsRequired().HasMaxLength(50);
            Property(u => u.TimeUntil).IsRequired().HasMaxLength(50);
            Property(u => u.WalkerId).IsRequired();
        }
    }
}
