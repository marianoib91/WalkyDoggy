using AutoMapper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkyDoggy.Application.Criterias;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;

namespace WalkyDoggy.Services.Services
{
    public class ServicioCiudades : ServicioEntidadBase<City, CityDto>, IServicioCiudades
    {
        #region Variables
        private readonly IRepositorioEntidadBase<City> repositorioCiudades;
        private readonly IRepositorioEntidadBase<Province> repositorioProvincias;
        private readonly IServicioEncriptacion servicioEncriptacion;
        private readonly IServicioMembresia servicioMembresia;
        #endregion

        public ServicioCiudades(IRepositorioEntidadBase<Error> repositorioErrores,
                                  IUnidadDeTrabajo unidadDeTrabajo,
                                  IRepositorioEntidadBase<City> repositorioCiudades,
                                  IRepositorioEntidadBase<Province> repositorioProvincias,
                                  IServicioEncriptacion servicioEncriptacion,
                                  IServicioMembresia servicioMembresia) :
            base(repositorioErrores, unidadDeTrabajo, repositorioCiudades)
        {
            this.repositorioCiudades = repositorioCiudades;
            this.repositorioProvincias = repositorioProvincias;
            this.servicioEncriptacion = servicioEncriptacion;
            this.servicioMembresia = servicioMembresia;
        }

        public List<CityDto> ObtenerTodos()
        {
            var ciudades = this.repositorioEntidad.ObtenerTodos().ToList();
            var ciudadesDto = Mapper.Map<List<City>, List<CityDto>>(ciudades);
            return ciudadesDto;
        }

        public List<CityDto> ObtenerTodosPorIdProvincia(Int64 idProvincia)
        {
            var ciudades = this.repositorioEntidad.ObtenerTodos().Where(x => x.ProvinceId == idProvincia).ToList();
            var ciudadesDto = Mapper.Map<List<City>, List<CityDto>>(ciudades);
            return ciudadesDto;
        }

        public CityDto Resolver(CityResolveCriteria criterioResolverCiudad)
        {
            if (criterioResolverCiudad == null ||
                String.IsNullOrWhiteSpace(criterioResolverCiudad.CityName) ||
                String.IsNullOrWhiteSpace(criterioResolverCiudad.ProvinceName))
            {
                return null;
            }

            var provincia = BuscarProvincia(criterioResolverCiudad.ProvinceName);
            if (provincia == null)
            {
                return null;
            }

            var nombreCiudad = criterioResolverCiudad.CityName.Trim();
            if (nombreCiudad.Length > 100)
            {
                nombreCiudad = nombreCiudad.Substring(0, 100);
            }

            var nombreCiudadNormalizado = Normalizar(nombreCiudad);
            var ciudad = this.repositorioCiudades.ObtenerTodos().
                                             Where(x => x.ProvinceId == provincia.Id).
                                             ToList().
                                             FirstOrDefault(x => Normalizar(x.Name) == nombreCiudadNormalizado);

            if (ciudad == null)
            {
                ciudad = new City
                {
                    ProvinceId = provincia.Id,
                    Name = nombreCiudad,
                    PostalCode = String.IsNullOrWhiteSpace(criterioResolverCiudad.PostalCode) ? "S/D" : criterioResolverCiudad.PostalCode.Trim()
                };
                this.repositorioCiudades.Agregar(ciudad);
                this.unidadDeTrabajo.GuardarCambios();
            }

            return Mapper.Map<City, CityDto>(ciudad);
        }

        private static readonly String[] NombresDeLaCiudadDeBuenosAires = { "autonomous city of buenos aires", "buenos aires city", "city of buenos aires", "capital federal", "ciudad de buenos aires", "caba", "c.a.b.a." };

        private Province BuscarProvincia(String provinceName)
        {
            var normalizado = Normalizar(provinceName).Replace("provincia de ", "");
            if (normalizado.EndsWith(" province"))
            {
                normalizado = normalizado.Substring(0, normalizado.Length - " province".Length);
            }

            //Nombres de la Ciudad de Buenos Aires que llegan en ingles o abreviados
            if (NombresDeLaCiudadDeBuenosAires.Contains(normalizado))
            {
                normalizado = "ciudad autonoma de buenos aires";
            }
            var provincias = this.repositorioProvincias.ObtenerTodos().ToList();

            //Coincidencia exacta y, si no, por prefijo (ej: "Tierra del Fuego" dentro del nombre completo de la provincia)
            return provincias.FirstOrDefault(x => Normalizar(x.Name) == normalizado) ??
                   provincias.FirstOrDefault(x => Normalizar(x.Name).StartsWith(normalizado));
        }

        //Minusculas, sin tildes y sin espacios repetidos, para comparar nombres escritos de distinta forma
        private static String Normalizar(String texto)
        {
            var descompuesto = (texto ?? String.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sinTildes = new String(descompuesto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
            return String.Join(" ", sinTildes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
