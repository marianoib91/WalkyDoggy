using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities.Dtos;

namespace WalkyDoggy.Application.Dtos
{
    public class WorkDayDto : IDto
    {
        public Int64 Id { get; set; }

        public String DayOfWeek { get; set; }

        public String TimeFrom { get; set; }

        public String TimeUntil { get; set; }

        public Int64 WalkerId { get; set; }
    }
}
