using System;
using System.Collections.Generic;
using System.Linq;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;

namespace WalkyDoggy.Services.Services
{
    public class ServicioModeracion : IServicioModeracion
    {
        private const Int32 MinimoMotivo = 5;
        private const Int32 MaximoMotivo = 300;
        private const Int32 MaximoPagina = 50;

        private readonly IRepositorioEntidadBase<Ranking> repositorioValoraciones;
        private readonly IRepositorioEntidadBase<PetReview> repositorioResenas;
        private readonly IRepositorioEntidadBase<Pet> repositorioMascotas;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;
        private readonly IRepositorioEntidadBase<AdminAction> repositorioAcciones;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;

        public ServicioModeracion(IRepositorioEntidadBase<Ranking> repositorioValoraciones,
                                  IRepositorioEntidadBase<PetReview> repositorioResenas,
                                  IRepositorioEntidadBase<Pet> repositorioMascotas,
                                  IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                  IRepositorioEntidadBase<AdminAction> repositorioAcciones,
                                  IUnidadDeTrabajo unidadDeTrabajo)
        {
            this.repositorioValoraciones = repositorioValoraciones;
            this.repositorioResenas = repositorioResenas;
            this.repositorioMascotas = repositorioMascotas;
            this.repositorioPaseadores = repositorioPaseadores;
            this.repositorioAcciones = repositorioAcciones;
            this.unidadDeTrabajo = unidadDeTrabajo;
        }

        /* ---------- Valoraciones de paseadores (las escriben los clientes) ---------- */

        public AdminRatingPageDto ListarValoraciones(String estado, Int32? maximoEstrellas, String buscar, Int32 pagina, Int32 tamanoPagina)
        {
            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Min(MaximoPagina, Math.Max(1, tamanoPagina));

            var valoraciones = this.repositorioValoraciones.TodosConIncluidos(x => x.Customer, x => x.Walker).ToList();

            IEnumerable<Ranking> consulta = valoraciones;
            if (estado == "Hidden") { consulta = consulta.Where(x => x.Hidden); }
            else if (estado == "Visible") { consulta = consulta.Where(x => !x.Hidden); }
            if (maximoEstrellas.HasValue) { consulta = consulta.Where(x => Math.Round(x.Score) <= maximoEstrellas.Value); }
            if (!String.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                consulta = consulta.Where(x => Contiene(x.Comments, texto) || Contiene(Nombre(x.Customer), texto) || Contiene(Nombre(x.Walker), texto));
            }

            var ordenadas = consulta.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToList();

            return new AdminRatingPageDto
            {
                Total = ordenadas.Count,
                Page = pagina,
                PageSize = tamanoPagina,
                Ratings = ordenadas.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).Select(x => new AdminRatingDto
                {
                    Id = x.Id,
                    Date = x.Date,
                    Stars = (Int32)Math.Round(x.Score),
                    Comment = x.Comments,
                    CustomerName = Nombre(x.Customer),
                    WalkerName = Nombre(x.Walker),
                    Hidden = x.Hidden,
                    HiddenReason = x.HiddenReason
                }).ToList()
            };
        }

        public Boolean OcultarValoracion(Int64 idAdministrador, HideReviewDto solicitud, out String error)
        {
            var valoracion = Buscar(solicitud, out error, id => this.repositorioValoraciones.TodosConIncluidos(x => x.Customer, x => x.Walker).FirstOrDefault(x => x.Id == id));
            if (valoracion == null)
            {
                return false;
            }
            if (valoracion.Hidden)
            {
                error = "La valoración ya está oculta.";
                return false;
            }

            var motivo = solicitud.Reason.Trim();
            valoracion.Hidden = true;
            valoracion.HiddenReason = motivo;
            valoracion.HiddenAt = DateTime.Now;
            Registrar(idAdministrador, AdminActionTypes.HideRating,
                      "Valoración de " + Nombre(valoracion.Customer) + " a " + Nombre(valoracion.Walker) + " (" + (Int32)Math.Round(valoracion.Score) + " estrellas) · Motivo: " + motivo);
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        public Boolean MostrarValoracion(Int64 idAdministrador, ShowReviewDto solicitud, out String error)
        {
            error = null;

            var valoracion = solicitud == null ? null : this.repositorioValoraciones.TodosConIncluidos(x => x.Customer, x => x.Walker).FirstOrDefault(x => x.Id == solicitud.Id);
            if (valoracion == null)
            {
                error = "La valoración no existe.";
                return false;
            }
            if (!valoracion.Hidden)
            {
                error = "La valoración no está oculta.";
                return false;
            }

            valoracion.Hidden = false;
            valoracion.HiddenReason = null;
            valoracion.HiddenAt = null;
            Registrar(idAdministrador, AdminActionTypes.ShowRating,
                      "Valoración de " + Nombre(valoracion.Customer) + " a " + Nombre(valoracion.Walker) + " vuelve a mostrarse");
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        /* ---------- Reseñas de mascotas (las escriben los paseadores) ---------- */

        public AdminPetReviewPageDto ListarResenasMascotas(String estado, Int32? maximoEstrellas, String buscar, Int32 pagina, Int32 tamanoPagina)
        {
            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Min(MaximoPagina, Math.Max(1, tamanoPagina));

            IEnumerable<PetReview> consulta = this.repositorioResenas.ObtenerTodos().ToList();
            if (estado == "Hidden") { consulta = consulta.Where(x => x.Hidden); }
            else if (estado == "Visible") { consulta = consulta.Where(x => !x.Hidden); }
            if (maximoEstrellas.HasValue) { consulta = consulta.Where(x => x.Stars <= maximoEstrellas.Value); }

            var resenas = consulta.ToList();
            var idsMascotas = resenas.Select(x => x.PetId).Distinct().ToList();
            var idsPaseadores = resenas.Select(x => x.WalkerId).Distinct().ToList();
            var mascotas = this.repositorioMascotas.BuscarPor(x => idsMascotas.Contains(x.Id)).ToList().ToDictionary(x => x.Id);
            var paseadores = this.repositorioPaseadores.BuscarPor(x => idsPaseadores.Contains(x.Id)).ToList().ToDictionary(x => x.Id);

            Func<PetReview, String> nombreMascota = x => mascotas.ContainsKey(x.PetId) ? mascotas[x.PetId].Name : "Mascota eliminada";
            Func<PetReview, String> nombrePaseador = x => paseadores.ContainsKey(x.WalkerId) ? Nombre(paseadores[x.WalkerId]) : "Paseador eliminado";

            if (!String.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                resenas = resenas.Where(x => Contiene(x.Comments, texto) || Contiene(nombreMascota(x), texto) || Contiene(nombrePaseador(x), texto)).ToList();
            }

            var ordenadas = resenas.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToList();

            return new AdminPetReviewPageDto
            {
                Total = ordenadas.Count,
                Page = pagina,
                PageSize = tamanoPagina,
                Reviews = ordenadas.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).Select(x => new AdminPetReviewDto
                {
                    Id = x.Id,
                    Date = x.Date,
                    Stars = x.Stars,
                    Comment = x.Comments,
                    WalkerName = nombrePaseador(x),
                    PetName = nombreMascota(x),
                    Hidden = x.Hidden,
                    HiddenReason = x.HiddenReason
                }).ToList()
            };
        }

        public Boolean OcultarResenaMascota(Int64 idAdministrador, HideReviewDto solicitud, out String error)
        {
            var resena = Buscar(solicitud, out error, id => this.repositorioResenas.ObtenerUno(id));
            if (resena == null)
            {
                return false;
            }
            if (resena.Hidden)
            {
                error = "La reseña ya está oculta.";
                return false;
            }

            var motivo = solicitud.Reason.Trim();
            resena.Hidden = true;
            resena.HiddenReason = motivo;
            resena.HiddenAt = DateTime.Now;
            Registrar(idAdministrador, AdminActionTypes.HidePetReview,
                      "Reseña de " + DescribirResena(resena) + " (" + resena.Stars + " estrellas) · Motivo: " + motivo);
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        public Boolean MostrarResenaMascota(Int64 idAdministrador, ShowReviewDto solicitud, out String error)
        {
            error = null;

            var resena = solicitud == null ? null : this.repositorioResenas.ObtenerUno(solicitud.Id);
            if (resena == null)
            {
                error = "La reseña no existe.";
                return false;
            }
            if (!resena.Hidden)
            {
                error = "La reseña no está oculta.";
                return false;
            }

            resena.Hidden = false;
            resena.HiddenReason = null;
            resena.HiddenAt = null;
            Registrar(idAdministrador, AdminActionTypes.ShowPetReview, "Reseña de " + DescribirResena(resena) + " vuelve a mostrarse");
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        /* ---------- Auxiliares ---------- */

        //Busca la reseña a ocultar y controla el motivo; devuelve null y el error si algo no esta bien
        private T Buscar<T>(HideReviewDto solicitud, out String error, Func<Int64, T> buscar) where T : class
        {
            error = null;

            if (solicitud == null)
            {
                error = "Faltan datos.";
                return null;
            }

            var motivo = (solicitud.Reason ?? String.Empty).Trim();
            if (motivo.Length < MinimoMotivo || motivo.Length > MaximoMotivo)
            {
                error = "Escribí por qué la ocultás (entre " + MinimoMotivo + " y " + MaximoMotivo + " caracteres).";
                return null;
            }

            var elemento = buscar(solicitud.Id);
            if (elemento == null)
            {
                error = "La reseña no existe.";
            }
            return elemento;
        }

        private String DescribirResena(PetReview resena)
        {
            var mascota = this.repositorioMascotas.ObtenerUno(resena.PetId);
            var paseador = this.repositorioPaseadores.ObtenerUno(resena.WalkerId);
            return (paseador != null ? Nombre(paseador) : "un paseador") + " sobre " + (mascota != null ? mascota.Name : "una mascota");
        }

        private static String Nombre(Customer cliente)
        {
            return cliente == null ? "Cliente eliminado" : ((cliente.FirstName ?? String.Empty) + " " + (cliente.LastName ?? String.Empty)).Trim();
        }

        private static String Nombre(Walker paseador)
        {
            return paseador == null ? "Paseador eliminado" : ((paseador.FirstName ?? String.Empty) + " " + (paseador.LastName ?? String.Empty)).Trim();
        }

        private static Boolean Contiene(String texto, String buscado)
        {
            return texto != null && texto.IndexOf(buscado, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private void Registrar(Int64 idAdministrador, String accion, String detalle)
        {
            this.repositorioAcciones.Agregar(new AdminAction
            {
                AdminUserId = idAdministrador,
                Action = accion,
                Detail = detalle.Length > 1000 ? detalle.Substring(0, 1000) : detalle,
                CreatedAt = DateTime.Now
            });
        }
    }
}
