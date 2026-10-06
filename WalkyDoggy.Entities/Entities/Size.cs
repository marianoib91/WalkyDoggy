using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Entities
{
    public class Size : ICatalogItem
    {
        public Int64 Id { get; set; }

        public String Name { get; set; }

        //Una raza o tamaño dado de baja ya no se ofrece al cargar mascotas, pero las que ya la tenian la conservan
        public Boolean Active { get; set; }

    }
}
