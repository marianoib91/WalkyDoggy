using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Entities.GoogleAPI;
using WalkyDoggy.Services.Contracts;

namespace WalkyDoggy.Services.Services
{
    public class GeocodeAppService : IGeocodeAppService
    {
        #region Variables
        #endregion

        public GeocodeDto GetLocation(Geocode geocode)
        {
            var address = geocode.StreetNumber+ geocode.StreetName+ geocode.CityName + geocode.ProvinceName;
            var apiKey = "AIzaSyAk3Csc2HZ03T26lmysEOJeMSEKOupMlzg";

            string requestUri = string.Format("https://maps.googleapis.com/maps/api/geocode/xml?address={0}&sensor=false&key={1}", Uri.EscapeDataString(address), Uri.EscapeDataString(apiKey));

            WebRequest request = WebRequest.Create(requestUri);
            WebResponse response = request.GetResponse();
            XDocument xdoc = XDocument.Load(response.GetResponseStream());

            XElement result = xdoc.Element("GeocodeResponse").Element("result");
            XElement locationElement = result.Element("geometry").Element("location");
            var lat = locationElement.Element("lat").Value;
            var lng = locationElement.Element("lng").Value;

            var geocodeDto = new GeocodeDto();
            
            geocodeDto.Latitude = lat;
            geocodeDto.Longitude = lng;

            return geocodeDto;
        }

    }
}
