using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities;

namespace WalkyDoggy.Entities
{
    public class Error : IEntityBase
    {
        public Int64 Id { get; set; }

        public String Message { get; set; }

        public String StackTrace { get; set; }

        public DateTime DateCreated { get; set; }
    }
}

