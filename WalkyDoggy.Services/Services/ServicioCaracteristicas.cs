using System;
using System.Collections.Generic;
using System.Linq;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;

namespace WalkyDoggy.Services.Services
{
    public class ServicioCaracteristicas : IServicioCaracteristicas
    {
        private readonly IRepositorioEntidadBase<PetTraitDefinition> repositorioCaracteristicas;

        public ServicioCaracteristicas(IRepositorioEntidadBase<PetTraitDefinition> repositorioCaracteristicas)
        {
            this.repositorioCaracteristicas = repositorioCaracteristicas;
        }

        public List<TraitPairDto> ObtenerPares()
        {
            return ArmarPares(this.repositorioCaracteristicas.ObtenerTodos().Where(x => x.Active).ToList());
        }

        public List<String> CodigosActivos()
        {
            return ObtenerPares().SelectMany(x => new[] { x.First.Code, x.Second.Code }).ToList();
        }

        public String Validar(String guardadas, out String normalizadas)
        {
            normalizadas = null;
            var elegidas = PetTraits.Leer(guardadas);
            if (elegidas.Count == 0)
            {
                return null;
            }

            var definiciones = this.repositorioCaracteristicas.ObtenerTodos().ToList();
            var conocidas = definiciones.Select(x => x.Code).ToList();
            if (elegidas.Any(x => !conocidas.Contains(x)))
            {
                return "Hay características de la mascota que no existen.";
            }

            //Las dadas de baja ya no se ofrecen: si la mascota todavia las tenia guardadas, se descartan
            var activas = definiciones.Where(x => x.Active).ToList();
            var vigentes = activas.Where(x => elegidas.Contains(x.Code)).ToList();

            if (vigentes.GroupBy(x => x.PairId).Any(par => par.Count() > 1))
            {
                return "De cada par de características solo se puede elegir una (por ejemplo, Juguetón o Tranquilo).";
            }

            var ordenadas = vigentes.OrderBy(x => x.PairId).ThenBy(x => x.Position).Select(x => x.Code).ToList();
            normalizadas = ordenadas.Count == 0 ? null : String.Join(",", ordenadas);
            return null;
        }

        //Arma los pares a partir de las filas; un par incompleto (sin sus dos caracteristicas) se ignora
        public static List<TraitPairDto> ArmarPares(List<PetTraitDefinition> definiciones)
        {
            return definiciones.
                GroupBy(x => x.PairId).
                Where(par => par.Count() == 2).
                OrderBy(par => par.Key).
                Select(par =>
                {
                    var primera = par.OrderBy(x => x.Position).First();
                    var segunda = par.OrderBy(x => x.Position).Last();
                    return new TraitPairDto
                    {
                        PairId = par.Key,
                        First = new TraitDto { Code = primera.Code, Label = primera.Label },
                        Second = new TraitDto { Code = segunda.Code, Label = segunda.Label },
                        Active = par.All(x => x.Active)
                    };
                }).
                ToList();
        }
    }
}
