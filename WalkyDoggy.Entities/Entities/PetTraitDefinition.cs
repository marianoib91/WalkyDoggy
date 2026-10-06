using System;

namespace WalkyDoggy.Entities
{
    //Una caracteristica con la que se describe a un perro. Viene en pares opuestos (dos filas con el mismo PairId):
    //de cada par se elige una sola. La administra el administrador; Code es lo que se guarda en la mascota y no cambia nunca.
    public class PetTraitDefinition : IEntityBase
    {
        public Int64 Id { get; set; }

        public String Code { get; set; }

        public String Label { get; set; }

        public Int64 PairId { get; set; }

        //1 o 2: cual de las dos del par es
        public Int32 Position { get; set; }

        //Una caracteristica dada de baja ya no se ofrece ni cuenta para el matching, pero las mascotas que la tenian la conservan guardada
        public Boolean Active { get; set; }
    }
}
