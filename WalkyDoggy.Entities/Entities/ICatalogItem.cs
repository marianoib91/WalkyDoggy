using System;

namespace WalkyDoggy.Entities
{
    //Un item de un catalogo simple que administra el administrador (razas y tamaños): tiene nombre y se puede dar de baja
    public interface ICatalogItem : IEntityBase
    {
        String Name { get; set; }

        Boolean Active { get; set; }
    }
}
