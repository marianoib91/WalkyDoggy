using System;
using System.Collections.Generic;

namespace WalkyDoggy.Application.Dtos
{
    //Una raza o un tamaño tal como lo ve el administrador
    public class AdminCatalogItemDto
    {
        public Int64 Id { get; set; }

        public String Name { get; set; }

        public Boolean Active { get; set; }

        //Cuantas mascotas la tienen cargada
        public Int32 PetCount { get; set; }

        //"Otro" es la opcion comodin: no se renombra ni se da de baja
        public Boolean Protected { get; set; }
    }

    //Id = 0 crea un item nuevo; con Id edita el nombre
    public class SaveCatalogItemDto
    {
        public Int64 Id { get; set; }

        public String Name { get; set; }
    }

    public class SetCatalogItemActiveDto
    {
        public Int64 Id { get; set; }

        public Boolean Active { get; set; }
    }

    public class TraitDto
    {
        public String Code { get; set; }

        public String Label { get; set; }
    }

    //Un par de caracteristicas opuestas (de cada par se elige una sola)
    public class TraitPairDto
    {
        public Int64 PairId { get; set; }

        public TraitDto First { get; set; }

        public TraitDto Second { get; set; }

        public Boolean Active { get; set; }

        //Cuantas mascotas tienen alguna de las dos
        public Int32 PetCount { get; set; }
    }

    //PairId = 0 crea un par nuevo; con PairId edita los textos
    public class SaveTraitPairDto
    {
        public Int64 PairId { get; set; }

        public String FirstLabel { get; set; }

        public String SecondLabel { get; set; }
    }

    public class SetTraitPairActiveDto
    {
        public Int64 PairId { get; set; }

        public Boolean Active { get; set; }
    }
}
