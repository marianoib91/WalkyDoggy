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
    public class ServicioValoraciones : IServicioValoraciones
    {
        public const Int32 LargoMaximoComentario = 500;
        private const Int32 TamanoMaximoPagina = 50;

        private readonly IRepositorioEntidadBase<Ranking> repositorioValoraciones;
        private readonly IRepositorioEntidadBase<Walk> repositorioPaseos;
        private readonly IRepositorioEntidadBase<Stay> repositorioHospedajes;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;

        public ServicioValoraciones(IRepositorioEntidadBase<Ranking> repositorioValoraciones,
                                IRepositorioEntidadBase<Walk> repositorioPaseos,
                                IRepositorioEntidadBase<Stay> repositorioHospedajes,
                                IUnidadDeTrabajo unidadDeTrabajo)
        {
            this.repositorioValoraciones = repositorioValoraciones;
            this.repositorioPaseos = repositorioPaseos;
            this.repositorioHospedajes = repositorioHospedajes;
            this.unidadDeTrabajo = unidadDeTrabajo;
        }

        public RatingSummaryDto ObtenerResumen(Int64 idPaseador)
        {
            var valoraciones = this.repositorioValoraciones.ObtenerTodos().
                                                  Where(x => x.WalkerId == idPaseador && !x.Hidden).
                                                  Select(x => new { x.Score, x.Comments }).
                                                  ToList();

            var resumen = new RatingSummaryDto
            {
                WalkerId = idPaseador,
                Count = valoraciones.Count,
                CommentCount = valoraciones.Count(x => !String.IsNullOrWhiteSpace(x.Comments)),
                Average = valoraciones.Count == 0 ? (Double?)null : Math.Round(valoraciones.Average(x => x.Score), 1),
                Distribution = new List<RatingBucketDto>()
            };

            for (var estrellas = 5; estrellas >= 1; estrellas--)
            {
                var actual = estrellas;
                resumen.Distribution.Add(new RatingBucketDto { Stars = estrellas, Count = valoraciones.Count(x => (Int32)Math.Round(x.Score) == actual) });
            }

            return resumen;
        }

        public RatingPageDto ObtenerValoraciones(Int64 idPaseador, String orden, Int32? estrellas, Int32 pagina, Int32 tamanoPagina)
        {
            pagina = Math.Max(pagina, 1);
            tamanoPagina = Math.Min(Math.Max(tamanoPagina, 1), TamanoMaximoPagina);

            var consulta = this.repositorioValoraciones.TodosConIncluidos(x => x.Customer).
                                                Where(x => x.WalkerId == idPaseador && !x.Hidden);
            if (estrellas.HasValue)
            {
                var estrellasBuscadas = (Double)estrellas.Value;
                consulta = consulta.Where(x => x.Score == estrellasBuscadas);
            }

            IOrderedQueryable<Ranking> ordenados;
            if (orden == "best")
            {
                ordenados = consulta.OrderByDescending(x => x.Score).ThenByDescending(x => x.Date);
            }
            else if (orden == "worst")
            {
                ordenados = consulta.OrderBy(x => x.Score).ThenByDescending(x => x.Date);
            }
            else
            {
                ordenados = consulta.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id);
            }

            var total = consulta.Count();
            var elementos = ordenados.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToList();

            return new RatingPageDto
            {
                Total = total,
                Page = pagina,
                HasMore = pagina * tamanoPagina < total,
                Items = elementos.Select(x => new RatingDto
                {
                    Id = x.Id,
                    Stars = (Int32)Math.Round(x.Score),
                    Comment = x.Comments,
                    AuthorName = NombreDelAutor(x.Customer),
                    Date = x.Date
                }).ToList()
            };
        }

        public Boolean Valorar(RateRequestDto solicitud, out String error)
        {
            error = null;

            if (solicitud == null || String.IsNullOrWhiteSpace(solicitud.BookingKey))
            {
                error = "Faltan datos de la reserva.";
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

            //Un hospedaje se valora igual que un paseo, cuando el cuidador devolvio a los perros
            Int64 idHospedaje;
            if (AyudanteReservas.EsClaveDeHospedaje(solicitud.BookingKey, out idHospedaje))
            {
                var hospedaje = this.repositorioHospedajes.ObtenerTodos().FirstOrDefault(x => x.Id == idHospedaje);
                if (hospedaje == null || hospedaje.CustomerId != solicitud.CustomerId)
                {
                    error = "El hospedaje no existe.";
                    return false;
                }
                if (hospedaje.Status != WalkStatus.Confirmed || !hospedaje.FinishedAt.HasValue)
                {
                    error = "Solo podés valorar un hospedaje cuando el cuidador devolvió a los perros.";
                    return false;
                }

                var claveHospedaje = solicitud.BookingKey;
                if (this.repositorioValoraciones.ObtenerTodos().Any(x => x.BookingKey == claveHospedaje))
                {
                    error = "Ya valoraste este hospedaje.";
                    return false;
                }

                this.repositorioValoraciones.Agregar(new Ranking
                {
                    WalkId = null,
                    WalkerId = hospedaje.WalkerId,
                    CustomerId = solicitud.CustomerId,
                    BookingKey = claveHospedaje,
                    Date = DateTime.Now,
                    Score = solicitud.Stars,
                    Comments = comentario.Length == 0 ? null : comentario
                });
                this.unidadDeTrabajo.GuardarCambios();
                return true;
            }

            var paseos = AyudanteReservas.Buscar(this.repositorioPaseos.TodosConIncluidos(x => x.Pet), solicitud.BookingKey);
            if (paseos.Count == 0 || paseos[0].Pet.CustomerId != solicitud.CustomerId)
            {
                error = "La reserva no existe.";
                return false;
            }

            if (paseos.Any(x => x.Status != WalkStatus.Confirmed || !x.FinishedAt.HasValue))
            {
                error = "Solo podés valorar un paseo cuando el paseador lo dio por finalizado.";
                return false;
            }

            var claveReserva = AyudanteReservas.ClaveDe(paseos[0]);
            if (this.repositorioValoraciones.ObtenerTodos().Any(x => x.BookingKey == claveReserva))
            {
                error = "Ya valoraste este paseo.";
                return false;
            }

            this.repositorioValoraciones.Agregar(new Ranking
            {
                WalkId = paseos[0].Id,
                WalkerId = paseos[0].WalkerId,
                CustomerId = solicitud.CustomerId,
                BookingKey = claveReserva,
                Date = DateTime.Now,
                Score = solicitud.Stars,
                Comments = comentario.Length == 0 ? null : comentario
            });
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        //"Mariano Baudena" se muestra como "Mariano B."
        private static String NombreDelAutor(Customer cliente)
        {
            if (cliente == null)
            {
                return "Cliente";
            }

            var apellido = (cliente.LastName ?? String.Empty).Trim();
            return (cliente.FirstName ?? "Cliente").Trim() + (apellido.Length > 0 ? " " + apellido.Substring(0, 1).ToUpper() + "." : String.Empty);
        }
    }
}
