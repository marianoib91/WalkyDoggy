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
    public class RatingAppService : IRatingAppService
    {
        public const Int32 MaxCommentLength = 500;
        private const Int32 MaxPageSize = 50;

        private readonly IEntityBaseRepository<Ranking> rankingsRepository;
        private readonly IEntityBaseRepository<Walk> walksRepository;
        private readonly IUnitOfWork unitOfWork;

        public RatingAppService(IEntityBaseRepository<Ranking> rankingsRepository,
                                IEntityBaseRepository<Walk> walksRepository,
                                IUnitOfWork unitOfWork)
        {
            this.rankingsRepository = rankingsRepository;
            this.walksRepository = walksRepository;
            this.unitOfWork = unitOfWork;
        }

        public RatingSummaryDto GetSummary(Int64 walkerId)
        {
            var ratings = this.rankingsRepository.GetAll().
                                                  Where(x => x.WalkerId == walkerId).
                                                  Select(x => new { x.Score, x.Comments }).
                                                  ToList();

            var summary = new RatingSummaryDto
            {
                WalkerId = walkerId,
                Count = ratings.Count,
                CommentCount = ratings.Count(x => !String.IsNullOrWhiteSpace(x.Comments)),
                Average = ratings.Count == 0 ? (Double?)null : Math.Round(ratings.Average(x => x.Score), 1),
                Distribution = new List<RatingBucketDto>()
            };

            for (var stars = 5; stars >= 1; stars--)
            {
                var current = stars;
                summary.Distribution.Add(new RatingBucketDto { Stars = stars, Count = ratings.Count(x => (Int32)Math.Round(x.Score) == current) });
            }

            return summary;
        }

        public RatingPageDto GetRatings(Int64 walkerId, String sort, Int32? stars, Int32 page, Int32 pageSize)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Min(Math.Max(pageSize, 1), MaxPageSize);

            var query = this.rankingsRepository.AllIncluding(x => x.Customer).
                                                Where(x => x.WalkerId == walkerId);
            if (stars.HasValue)
            {
                var wanted = (Double)stars.Value;
                query = query.Where(x => x.Score == wanted);
            }

            IOrderedQueryable<Ranking> ordered;
            if (sort == "best")
            {
                ordered = query.OrderByDescending(x => x.Score).ThenByDescending(x => x.Date);
            }
            else if (sort == "worst")
            {
                ordered = query.OrderBy(x => x.Score).ThenByDescending(x => x.Date);
            }
            else
            {
                ordered = query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id);
            }

            var total = query.Count();
            var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new RatingPageDto
            {
                Total = total,
                Page = page,
                HasMore = page * pageSize < total,
                Items = items.Select(x => new RatingDto
                {
                    Id = x.Id,
                    Stars = (Int32)Math.Round(x.Score),
                    Comment = x.Comments,
                    AuthorName = AuthorNameOf(x.Customer),
                    Date = x.Date
                }).ToList()
            };
        }

        public Boolean Rate(RateRequestDto request, out String error)
        {
            error = null;

            if (request == null || String.IsNullOrWhiteSpace(request.BookingKey))
            {
                error = "Faltan datos de la reserva.";
                return false;
            }

            if (request.Stars < 1 || request.Stars > 5)
            {
                error = "Elegí de 1 a 5 estrellas.";
                return false;
            }

            var comment = (request.Comment ?? String.Empty).Trim();
            if (comment.Length > MaxCommentLength)
            {
                error = "El comentario puede tener hasta " + MaxCommentLength + " caracteres.";
                return false;
            }

            var walks = BookingHelper.Find(this.walksRepository.AllIncluding(x => x.Pet), request.BookingKey);
            if (walks.Count == 0 || walks[0].Pet.CustomerId != request.CustomerId)
            {
                error = "La reserva no existe.";
                return false;
            }

            if (walks.Any(x => x.Status != WalkStatus.Confirmed || !x.FinishedAt.HasValue))
            {
                error = "Solo podés valorar un paseo cuando el paseador lo dio por finalizado.";
                return false;
            }

            var bookingKey = BookingHelper.KeyOf(walks[0]);
            if (this.rankingsRepository.GetAll().Any(x => x.BookingKey == bookingKey))
            {
                error = "Ya valoraste este paseo.";
                return false;
            }

            this.rankingsRepository.Add(new Ranking
            {
                WalkId = walks[0].Id,
                WalkerId = walks[0].WalkerId,
                CustomerId = request.CustomerId,
                BookingKey = bookingKey,
                Date = DateTime.Now,
                Score = request.Stars,
                Comments = comment.Length == 0 ? null : comment
            });
            this.unitOfWork.Commit();
            return true;
        }

        //"Mariano Baudena" se muestra como "Mariano B."
        private static String AuthorNameOf(Customer customer)
        {
            if (customer == null)
            {
                return "Cliente";
            }

            var lastName = (customer.LastName ?? String.Empty).Trim();
            return (customer.FirstName ?? "Cliente").Trim() + (lastName.Length > 0 ? " " + lastName.Substring(0, 1).ToUpper() + "." : String.Empty);
        }
    }
}
