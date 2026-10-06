using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;

namespace WalkyDoggy.Services.Services
{
    public class ServicioCatalogos : IServicioCatalogos
    {
        private const Int32 MinimoNombre = 2;
        private const Int32 MaximoNombre = 60;
        private const Int32 MaximoCaracteristica = 40;

        //La opcion comodin de las razas: no se renombra ni se da de baja, y en las pantallas va siempre al final
        private const String RazaComodin = "Otro";

        private readonly IRepositorioEntidadBase<Breed> repositorioRazas;
        private readonly IRepositorioEntidadBase<Size> repositorioTamanos;
        private readonly IRepositorioEntidadBase<Pet> repositorioMascotas;
        private readonly IRepositorioEntidadBase<PetTraitDefinition> repositorioCaracteristicas;
        private readonly IRepositorioEntidadBase<AdminAction> repositorioAcciones;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;

        public ServicioCatalogos(IRepositorioEntidadBase<Breed> repositorioRazas,
                                 IRepositorioEntidadBase<Size> repositorioTamanos,
                                 IRepositorioEntidadBase<Pet> repositorioMascotas,
                                 IRepositorioEntidadBase<PetTraitDefinition> repositorioCaracteristicas,
                                 IRepositorioEntidadBase<AdminAction> repositorioAcciones,
                                 IUnidadDeTrabajo unidadDeTrabajo)
        {
            this.repositorioRazas = repositorioRazas;
            this.repositorioTamanos = repositorioTamanos;
            this.repositorioMascotas = repositorioMascotas;
            this.repositorioCaracteristicas = repositorioCaracteristicas;
            this.repositorioAcciones = repositorioAcciones;
            this.unidadDeTrabajo = unidadDeTrabajo;
        }

        /* ---------- Razas y tamaños ---------- */

        public List<AdminCatalogItemDto> ListarRazas()
        {
            var cantidades = this.repositorioMascotas.ObtenerTodos().GroupBy(x => x.BreedId).
                Select(x => new { Id = x.Key, Cantidad = x.Count() }).ToList().ToDictionary(x => x.Id, x => x.Cantidad);
            return Listar(this.repositorioRazas, cantidades, true, true);
        }

        public Boolean GuardarRaza(Int64 idAdministrador, SaveCatalogItemDto solicitud, out String error)
        {
            return Guardar(this.repositorioRazas, idAdministrador, solicitud, AdminActionTypes.SaveBreed, "Raza", true, out error);
        }

        public Boolean EstablecerRazaActiva(Int64 idAdministrador, SetCatalogItemActiveDto solicitud, out String error)
        {
            return EstablecerActivo(this.repositorioRazas, idAdministrador, solicitud, AdminActionTypes.SetBreedActive, "Raza", true, out error);
        }

        public List<AdminCatalogItemDto> ListarTamanos()
        {
            var cantidades = this.repositorioMascotas.ObtenerTodos().GroupBy(x => x.SizeId).
                Select(x => new { Id = x.Key, Cantidad = x.Count() }).ToList().ToDictionary(x => x.Id, x => x.Cantidad);
            return Listar(this.repositorioTamanos, cantidades, false, false);
        }

        public Boolean GuardarTamano(Int64 idAdministrador, SaveCatalogItemDto solicitud, out String error)
        {
            return Guardar(this.repositorioTamanos, idAdministrador, solicitud, AdminActionTypes.SaveSize, "Tamaño", false, out error);
        }

        public Boolean EstablecerTamanoActivo(Int64 idAdministrador, SetCatalogItemActiveDto solicitud, out String error)
        {
            return EstablecerActivo(this.repositorioTamanos, idAdministrador, solicitud, AdminActionTypes.SetSizeActive, "Tamaño", false, out error);
        }

        private static Boolean EsComodin(ICatalogItem item, Boolean hayComodin)
        {
            return hayComodin && String.Equals(item.Name, RazaComodin, StringComparison.OrdinalIgnoreCase);
        }

        private static List<AdminCatalogItemDto> Listar<T>(IRepositorioEntidadBase<T> repositorio, Dictionary<Int64, Int32> cantidades, Boolean hayComodin, Boolean ordenarPorNombre)
            where T : class, ICatalogItem, new()
        {
            return repositorio.ObtenerTodos().ToList().
                OrderBy(x => EsComodin(x, hayComodin)).
                //Las razas van alfabeticas; los tamaños en el orden en que se cargaron (de chico a grande)
                ThenBy(x => ordenarPorNombre ? x.Name : String.Empty, StringComparer.CurrentCultureIgnoreCase).
                ThenBy(x => x.Id).
                Select(x => new AdminCatalogItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Active = x.Active,
                    PetCount = cantidades.ContainsKey(x.Id) ? cantidades[x.Id] : 0,
                    Protected = EsComodin(x, hayComodin)
                }).
                ToList();
        }

        private Boolean Guardar<T>(IRepositorioEntidadBase<T> repositorio, Int64 idAdministrador, SaveCatalogItemDto solicitud, String accion, String etiqueta, Boolean hayComodin, out String error)
            where T : class, ICatalogItem, new()
        {
            error = null;

            var nombre = Normalizar(solicitud == null ? null : solicitud.Name);
            if (nombre.Length < MinimoNombre || nombre.Length > MaximoNombre)
            {
                error = "El nombre tiene que tener entre " + MinimoNombre + " y " + MaximoNombre + " caracteres.";
                return false;
            }

            var existentes = repositorio.ObtenerTodos().ToList();
            if (existentes.Any(x => x.Id != solicitud.Id && String.Equals(x.Name, nombre, StringComparison.CurrentCultureIgnoreCase)))
            {
                error = "Ya existe un item con ese nombre.";
                return false;
            }

            if (solicitud.Id == 0)
            {
                repositorio.Agregar(new T { Name = nombre, Active = true });
                Registrar(idAdministrador, accion, etiqueta + " creado: " + nombre);
            }
            else
            {
                var item = existentes.FirstOrDefault(x => x.Id == solicitud.Id);
                if (item == null)
                {
                    error = "El item no existe.";
                    return false;
                }
                if (EsComodin(item, hayComodin))
                {
                    error = "\"" + RazaComodin + "\" es la opción comodín y no se puede renombrar.";
                    return false;
                }
                if (item.Name == nombre)
                {
                    return true;
                }

                Registrar(idAdministrador, accion, etiqueta + " renombrado: " + item.Name + " → " + nombre);
                item.Name = nombre;
            }

            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        private Boolean EstablecerActivo<T>(IRepositorioEntidadBase<T> repositorio, Int64 idAdministrador, SetCatalogItemActiveDto solicitud, String accion, String etiqueta, Boolean hayComodin, out String error)
            where T : class, ICatalogItem, new()
        {
            error = null;

            var item = solicitud == null ? null : repositorio.ObtenerUno(solicitud.Id);
            if (item == null)
            {
                error = "El item no existe.";
                return false;
            }
            if (!solicitud.Active && EsComodin(item, hayComodin))
            {
                error = "\"" + RazaComodin + "\" es la opción comodín y no se puede dar de baja.";
                return false;
            }
            if (item.Active == solicitud.Active)
            {
                return true;
            }

            item.Active = solicitud.Active;
            Registrar(idAdministrador, accion, etiqueta + (solicitud.Active ? " reactivado: " : " dado de baja: ") + item.Name);
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        public Boolean RazaYTamanoDisponibles(Int64 idMascota, Int64 idRaza, Int64 idTamano, out String error)
        {
            error = null;

            var raza = this.repositorioRazas.ObtenerUno(idRaza);
            var tamano = this.repositorioTamanos.ObtenerUno(idTamano);
            if (raza == null || tamano == null)
            {
                error = raza == null ? "La raza elegida no existe." : "El tamaño elegido no existe.";
                return false;
            }

            Int64 razaActual = 0, tamanoActual = 0;
            if (idMascota > 0)
            {
                var actual = this.repositorioMascotas.BuscarPor(x => x.Id == idMascota).Select(x => new { x.BreedId, x.SizeId }).FirstOrDefault();
                if (actual != null)
                {
                    razaActual = actual.BreedId;
                    tamanoActual = actual.SizeId;
                }
            }

            if (!raza.Active && raza.Id != razaActual)
            {
                error = "La raza elegida ya no está disponible. Elegí otra.";
                return false;
            }
            if (!tamano.Active && tamano.Id != tamanoActual)
            {
                error = "El tamaño elegido ya no está disponible. Elegí otro.";
                return false;
            }

            return true;
        }

        /* ---------- Caracteristicas ---------- */

        public List<TraitPairDto> ListarCaracteristicas()
        {
            var pares = ServicioCaracteristicas.ArmarPares(this.repositorioCaracteristicas.ObtenerTodos().ToList());
            var guardadas = this.repositorioMascotas.ObtenerTodos().Where(x => x.Traits != null && x.Traits != "").Select(x => x.Traits).ToList().
                Select(x => WalkyDoggy.Application.Constants.PetTraits.Leer(x)).ToList();

            foreach (var par in pares)
            {
                par.PetCount = guardadas.Count(x => x.Contains(par.First.Code) || x.Contains(par.Second.Code));
            }

            return pares;
        }

        public Boolean GuardarCaracteristicas(Int64 idAdministrador, SaveTraitPairDto solicitud, out String error)
        {
            error = null;

            var primera = Normalizar(solicitud == null ? null : solicitud.FirstLabel);
            var segunda = Normalizar(solicitud == null ? null : solicitud.SecondLabel);
            if (primera.Length < MinimoNombre || primera.Length > MaximoCaracteristica || segunda.Length < MinimoNombre || segunda.Length > MaximoCaracteristica)
            {
                error = "Cada característica tiene que tener entre " + MinimoNombre + " y " + MaximoCaracteristica + " caracteres.";
                return false;
            }
            if (String.Equals(primera, segunda, StringComparison.CurrentCultureIgnoreCase))
            {
                error = "Las dos características del par tienen que ser distintas (son opuestas).";
                return false;
            }

            var todas = this.repositorioCaracteristicas.ObtenerTodos().ToList();
            var ajenas = todas.Where(x => x.PairId != solicitud.PairId).ToList();
            if (ajenas.Any(x => String.Equals(x.Label, primera, StringComparison.CurrentCultureIgnoreCase) ||
                                String.Equals(x.Label, segunda, StringComparison.CurrentCultureIgnoreCase)))
            {
                error = "Ya existe una característica con ese texto.";
                return false;
            }

            if (solicitud.PairId == 0)
            {
                var idPar = todas.Count == 0 ? 1 : todas.Max(x => x.PairId) + 1;
                this.repositorioCaracteristicas.Agregar(new PetTraitDefinition { Code = NuevoCodigo(), Label = primera, PairId = idPar, Position = 1, Active = true });
                this.repositorioCaracteristicas.Agregar(new PetTraitDefinition { Code = NuevoCodigo(), Label = segunda, PairId = idPar, Position = 2, Active = true });
                Registrar(idAdministrador, AdminActionTypes.SaveTraitPair, "Par creado: " + primera + " / " + segunda);
            }
            else
            {
                var filas = todas.Where(x => x.PairId == solicitud.PairId).OrderBy(x => x.Position).ToList();
                if (filas.Count != 2)
                {
                    error = "El par de características no existe.";
                    return false;
                }
                if (filas[0].Label == primera && filas[1].Label == segunda)
                {
                    return true;
                }

                Registrar(idAdministrador, AdminActionTypes.SaveTraitPair,
                          "Par modificado: " + filas[0].Label + " / " + filas[1].Label + " → " + primera + " / " + segunda);
                filas[0].Label = primera;
                filas[1].Label = segunda;
            }

            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        public Boolean EstablecerParActivo(Int64 idAdministrador, SetTraitPairActiveDto solicitud, out String error)
        {
            error = null;

            var filas = solicitud == null ? new List<PetTraitDefinition>()
                                          : this.repositorioCaracteristicas.BuscarPor(x => x.PairId == solicitud.PairId).OrderBy(x => x.Position).ToList();
            if (filas.Count != 2)
            {
                error = "El par de características no existe.";
                return false;
            }
            if (filas.All(x => x.Active == solicitud.Active))
            {
                return true;
            }

            filas.ForEach(x => x.Active = solicitud.Active);
            Registrar(idAdministrador, AdminActionTypes.SetTraitPairActive,
                      "Par " + (solicitud.Active ? "reactivado: " : "dado de baja: ") + filas[0].Label + " / " + filas[1].Label);
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        /* ---------- Auxiliares ---------- */

        //Sin espacios de mas (al principio, al final ni repetidos)
        private static String Normalizar(String texto)
        {
            return Regex.Replace((texto ?? String.Empty).Trim(), @"\s+", " ");
        }

        //El codigo es lo que se guarda en la mascota: no depende del texto, asi que renombrar no rompe nada
        private String NuevoCodigo()
        {
            return "T" + Guid.NewGuid().ToString("N").Substring(0, 10);
        }

        private void Registrar(Int64 idAdministrador, String accion, String detalle)
        {
            this.repositorioAcciones.Agregar(new AdminAction
            {
                AdminUserId = idAdministrador,
                Action = accion,
                Detail = detalle,
                CreatedAt = DateTime.Now
            });
        }
    }
}
