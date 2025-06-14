using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Entities.GoogleAPI;

namespace WalkyDoggy.Services.Contracts
{
    public interface IGeocodeAppService
    {
        GeocodeDto GetLocation(Geocode geocode);

    }
}
