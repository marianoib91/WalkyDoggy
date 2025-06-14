using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Entities.GoogleAPI
{
    public class Geocode
    {
        public String StreetName { get; set; }

        public String StreetNumber { get; set; }

        public String CityName { get; set; }

        public String ProvinceName { get; set; }

        public String LatitudeLongitude { get; set; }
    }
}
