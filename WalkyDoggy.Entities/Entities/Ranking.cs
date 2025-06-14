using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Entities
{
    public class Ranking : IEntityBase
    {
        public Int64 Id { get; set; }

        public Int64 WalkId { get; set; }

        public DateTime Date { get; set; }

        public Double Score { get; set; }

        public String Comments { get; set; }

        public Walk Walk { get; set; }



    }
}
