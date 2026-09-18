using System;
using System.Collections.Generic;

namespace WalkyDoggy.Application.Criterias
{
    public class WalkRequestCriteria
    {
        public Int64 WalkerId { get; set; }

        //Formato yyyy-MM-dd
        public String Date { get; set; }

        //Formato HH:00
        public String TimeFrom { get; set; }

        public String Details { get; set; }

        public List<Int64> PetIds { get; set; }

        //Direccion de retiro. Si no se informa se usa la del domicilio del cliente.
        public String PickupStreetName { get; set; }

        public Int64? PickupStreetNumber { get; set; }

        public Int64? PickupCityId { get; set; }

        public String PickupLatitude { get; set; }

        public String PickupLongitude { get; set; }
    }
}
