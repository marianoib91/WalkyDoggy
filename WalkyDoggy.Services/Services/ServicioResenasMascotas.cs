using System;
using System.Collections.Generic;
using System.Linq;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Services.Utilities;

namespace WalkyDoggy.Services.Services
{
    public class ServicioResenasMascotas : IServicioResenasMascotas
    {
        public const Int32 LargoMaximoComentario = 500;
        public const Int32 EstrellasNegativas = 2;

        private readonly IRepositorioEntidadBase<PetReview> repositorioResenas;
        private readonly IRepositorioEntidadBase<Walk> repositorioPaseos;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;

        public ServicioResenasMascotas(IRepositorioEntidadBase<PetReview> repositorioResenas,
                                       IRepositorioEntidadBase<Walk> repositorioPaseos,
                                       IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                       IUnidadDeTrabajo unidadDeTrabajo)
        {
            this.repositorioResenas = repositorioResenas;
            this.repositorioPaseos = repositorioPaseos;
            this.repositorioPaseadores = repositorioPaseadores;
            this.unidadDeTrabajo = unidadDeTrabajo;
        }

        public Boolean Resenar(PetReviewRequestDto solicitud, out String error)
        {
            error = null;

            if (solicitud == null || String.IsNullOrWhiteSpace(solicitud.BookingKey))
            {
                error = "Faltan datos del paseo.";
                return false;
            }

            if (solicitud.Stars < 1 || solicitud.Stars > 5)
            {
                error = "Elegí de 1 a 5 estrellas.";
                return false;
            }

            var comentario = (solicitud.Comment ?? String.Empty).Trim();
            if (comentario.Length > LargoMaximoComentario)
            {
                error = "El comentario puede tener hasta " + LargoMaximoComentario + " caracteres.";
                return false;
            }

            var paseos = AyudanteReservas.Buscar(this.repositorioPaseos.TodosConIncluidos(x => x.Pet), solicitud.BookingKey);
            if (paseos.Count == 0 || paseos[0].WalkerId != solicitud.WalkerId)
            {
                error = "El paseo no existe.";
                return false;
            }

            var paseo = paseos.FirstOrDefault(x => x.PetId == solicitud.PetId);
            if (paseo == null)
            {
                error = "Esa mascota no estaba en el paseo.";
                return false;
            }

            if (paseo.Status != WalkStatus.Confirmed || !paseo.FinishedAt.HasValue)
            {
                error = "Solo podés reseñar a una mascota cuando diste el paseo por finalizado.";
                return false;
            }

            if (this.repositorioResenas.ObtenerTodos().Any(x => x.WalkId == paseo.Id))
            {
                error = "Ya reseñaste a " + paseo.Pet.Name + " en este paseo.";
                return false;
            }

            this.repositorioResenas.Agregar(new PetReview
            {
                WalkId = paseo.Id,
                PetId = paseo.PetId,
                WalkerId = paseo.WalkerId,
                BookingKey = AyudanteReservas.ClaveDe(paseo),
                Date = DateTime.Now,
                Stars = solicitud.Stars,
                Comments = comentario.Length == 0 ? null : comentario
            });
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        public PetReviewSummaryDto ObtenerResenas(Int64 idMascota, Int64 idPaseadorQueConsulta, Int32 maximo)
        {
            var resenas = this.repositorioResenas.ObtenerTodos().
                                                  Where(x => x.PetId == idMascota).
                                                  OrderByDescending(x => x.Date).
                                                  ToList();

            var resumen = Resumir(idMascota, resenas);

            var idsPaseadores = resenas.Select(x => x.WalkerId).Distinct().ToList();
            var paseadores = this.repositorioPaseadores.BuscarPor(x => idsPaseadores.Contains(x.Id)).ToList().ToDictionary(x => x.Id);

            resumen.Reviews = resenas.Take(maximo).Select(x => new PetReviewDto
            {
                Stars = x.Stars,
                Comment = x.Comments,
                Date = x.Date,
                WalkerName = NombreDelAutor(paseadores.ContainsKey(x.WalkerId) ? paseadores[x.WalkerId] : null),
                IsMine = x.WalkerId == idPaseadorQueConsulta
            }).ToList();

            return resumen;
        }

        public Dictionary<Int64, PetReviewSummaryDto> ObtenerResumenes(IEnumerable<Int64> idsMascotas)
        {
            var ids = idsMascotas.Distinct().ToList();
            var resenas = this.repositorioResenas.BuscarPor(x => ids.Contains(x.PetId)).ToList();

            return resenas.GroupBy(x => x.PetId).ToDictionary(x => x.Key, x => Resumir(x.Key, x.ToList()));
        }

        public HashSet<Int64> ObtenerPaseosResenados(IEnumerable<Int64> idsPaseos, Int64 idPaseador)
        {
            var ids = idsPaseos.Distinct().ToList();
            return new HashSet<Int64>(this.repositorioResenas.BuscarPor(x => ids.Contains(x.WalkId) && x.WalkerId == idPaseador).Select(x => x.WalkId).ToList());
        }

        private static PetReviewSummaryDto Resumir(Int64 idMascota, List<PetReview> resenas)
        {
            return new PetReviewSummaryDto
            {
                PetId = idMascota,
                Count = resenas.Count,
                Average = resenas.Count == 0 ? (Double?)null : Math.Round(resenas.Average(x => (Double)x.Stars), 1),
                NegativeCount = resenas.Count(x => x.Stars <= EstrellasNegativas),
                Reviews = new List<PetReviewDto>()
            };
        }

        //"Camila Rodríguez" se muestra como "Camila R."
        private static String NombreDelAutor(Walker paseador)
        {
            if (paseador == null)
            {
                return "Un paseador";
            }

            var inicial = String.IsNullOrWhiteSpace(paseador.LastName) ? String.Empty : " " + paseador.LastName.Trim().Substring(0, 1).ToUpper() + ".";
            return (paseador.FirstName ?? String.Empty).Trim() + inicial;
        }
    }
}
