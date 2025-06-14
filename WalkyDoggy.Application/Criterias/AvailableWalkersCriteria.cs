using System;
using System.Collections.Generic;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Application.Criterias
{
    public class AvailableWalkersCriteria
    {
        public String Date { get; set; }

        public String TimeFrom { get; set; }

        public List<PetDto> SelectedPets { get; set; }
    }
}
