using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Entities
{
    public class Walker : IEntityBase
    {
        public Int64 Id { get; set; }

        public Int64 UserId { get; set; }

        public Int64 CityId { get; set; }

        public Int64 PriceId { get; set; }

        public String FirstName { get; set; }

        public String LastName { get; set; }

        public String Email { get; set; }

        public String Description { get; set; }

        public String Phone { get; set; }

        public String StreetName { get; set; }

        public Int64 StreetNumber { get; set; }

        public String ProfileImage { get; set; }

        public User User { get; set; }

        public City City { get; set; }

        public Price Price { get; set; }
    }
}
