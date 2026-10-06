using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Entities.Dtos;

namespace WalkyDoggy.Application.Dtos
{
    public class WalkerDto : IDto
    {
        public Int64 Id { get; set; }

        public Int64 UserId { get; set; }

        public Int64 PriceId { get; set; }

        public Int64 CityId { get; set; }

        public Int64 RoleId { get; set; }

        public Int64 ProvinceId { get; set; }

        public String FirstName { get; set; }

        public String LastName { get; set; }

        public String Email { get; set; }

        public String Description { get; set; }

        public String Phone { get; set; }

        public String StreetName { get; set; }

        public Int64 StreetNumber { get; set; }

        public String ProfileImage { get; set; }

        public String Latitude { get; set; }

        public String Longitude { get; set; }

        public String Password { get; set; }

        public String ProvinceName { get; set; }

        public String CityName { get; set; }

        public Double Amount { get; set; }

        //Radio de trabajo, en km, alrededor de la direccion de referencia (StreetName, StreetNumber, Latitude, Longitude)
        public Double ServiceRadiusKm { get; set; }

        //Cuantas mascotas lleva a la vez en un mismo horario (de 1 a 5)
        public Int32 MaxPetsAtOnce { get; set; }

        //Alias o CBU/CVU para recibir transferencias (opcional)
        public String PayoutAccount { get; set; }

        //Hospedaje que ofrece en su casa (se muestra en su perfil publico; se edita desde Condiciones laborales)
        public Boolean BoardingEnabled { get; set; }

        public Decimal? BoardingPricePerNight { get; set; }

        public Int32 BoardingMaxDogs { get; set; }

        public String BoardingDescription { get; set; }

        //Distancia en km a la direccion de retiro (solo cuando se busca paseadores para una direccion)
        public Double? DistanceKm { get; set; }

        //Indica si el paseador vinculo su cuenta de Mercado Pago (los tokens nunca viajan al navegador)
        public Boolean MercadoPagoLinked { get; set; }

        //Valoracion promedio (null si todavia no tiene) y cantidad de valoraciones
        public Double? AverageRating { get; set; }

        public Int32 RatingCount { get; set; }

        //Horarios libres del dia buscado, con los lugares que le quedan en cada uno (solo al buscar paseadores por dia)
        public List<AvailableTimeDto> AvailableTimes { get; set; }

        //Matching entre mascotas (solo al buscar por dia con las mascotas del cliente): los datos de su mejor horario
        public Int32 MatchingPets { get; set; }

        public Int32 MatchScore { get; set; }

        public List<String> SharedTraits { get; set; }

        //El perro mas parecido que lleva en ese mejor horario
        public PetMatchDto BestMatch { get; set; }
    }

    //El perro de otro cliente que mas se parece a una mascota del cliente que busca (solo el perro: no se informa de quien es)
    public class PetMatchDto
    {
        //La mascota del cliente con la que coincide (importa cuando busco con mas de una)
        public Int64 YourPetId { get; set; }

        public String YourPetName { get; set; }

        public String PetName { get; set; }

        public String BreedName { get; set; }

        public String SizeName { get; set; }

        public Int32 SharedCount { get; set; }

        public List<String> SharedTraits { get; set; }
    }

    //Un horario con lugares libres de un paseador
    public class AvailableTimeDto
    {
        //Dia del horario (yyyy-MM-dd)
        public String Date { get; set; }

        public String Time { get; set; }

        public Int32 FreeSpots { get; set; }

        //Cuantos perros de otros clientes que el paseador lleva en ese horario tienen caracteristicas parecidas a las de las mascotas del cliente
        public Int32 MatchingPets { get; set; }

        //Cuantas caracteristicas tiene en comun el perro que mas se parece (0 si no hay ninguno parecido)
        public Int32 MatchScore { get; set; }

        //Las caracteristicas (codigos) que comparten todos los perros parecidos de ese horario
        public List<String> SharedTraits { get; set; }

        //El perro que mas se parece en ese horario y con cual de las mascotas del cliente (null si no hay ninguno parecido). No se informa de quien es.
        public PetMatchDto BestMatch { get; set; }
    }
}
