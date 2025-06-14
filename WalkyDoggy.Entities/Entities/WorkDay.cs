using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Entities.Entities
{
    public class WorkDay : IEntityBase
    {
        public Int64 Id { get; set; }

        public String DayOfWeek { get; set; }

        public String TimeFrom { get; set; }

        public String TimeUntil { get; set; }

        public Int64 WalkerId { get; set; }

        public Walker Walker { get; set; }
    }
}
