using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.Restaurants;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class RestaurantRatingService : IRestaurantRatingService
{
    private readonly AppDbContext _dbContext;

    public RestaurantRatingService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RestaurantRatingDto> UpsertRatingAsync(
        Guid userId,
        Guid restaurantId,
        SubmitRestaurantRatingRequestDto request,
        CancellationToken cancellationToken)
    {
        if (restaurantId == Guid.Empty)
        {
            throw BuildValidationException("restaurantId", "Restaurant id is required.");
        }

        ArgumentNullException.ThrowIfNull(request);

        var userEligible = await _dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == userId
                    && user.IsActive
                    && user.Role == UserRole.User,
                cancellationToken);

        if (!userEligible)
        {
            throw new RestaurantRatingServiceException(
                "Authenticated user account was not found or is not allowed to rate restaurants.",
                StatusCodes.Status401Unauthorized);
        }

        var restaurantExists = await _dbContext.Restaurants
            .AsNoTracking()
            .AnyAsync(
                restaurant => restaurant.Id == restaurantId
                    && restaurant.ApprovalStatus == RestaurantApprovalStatus.Approved,
                cancellationToken);

        if (!restaurantExists)
        {
            throw new RestaurantRatingServiceException(
                "Restaurant was not found or is not available for rating.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["The selected restaurant was not found or is not approved."]
                });
        }

        var rating = await _dbContext.RestaurantRatings
            .Include(entity => entity.Restaurant)
            .ThenInclude(restaurant => restaurant!.Ratings)
            .FirstOrDefaultAsync(
                entity => entity.UserId == userId && entity.RestaurantId == restaurantId,
                cancellationToken);

        var nowUtc = DateTime.UtcNow;
        if (rating is null)
        {
            rating = new RestaurantRating
            {
                Id = Guid.NewGuid(),
                RestaurantId = restaurantId,
                UserId = userId,
                Stars = request.Stars,
                Comment = NormalizeComment(request.Comment),
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            };

            _dbContext.RestaurantRatings.Add(rating);
        }
        else
        {
            rating.Stars = request.Stars;
            rating.Comment = NormalizeComment(request.Comment);
            rating.UpdatedAtUtc = nowUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (rating.Restaurant is null)
        {
            await _dbContext.Entry(rating)
                .Reference(entity => entity.Restaurant)
                .Query()
                .Include(restaurant => restaurant.Ratings)
                .LoadAsync(cancellationToken);
        }

        return MapRating(rating);
    }

    public async Task<RestaurantRatingDto?> GetUserRatingAsync(
        Guid userId,
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        if (restaurantId == Guid.Empty)
        {
            throw BuildValidationException("restaurantId", "Restaurant id is required.");
        }

        var userEligible = await _dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == userId
                    && user.IsActive
                    && user.Role == UserRole.User,
                cancellationToken);

        if (!userEligible)
        {
            throw new RestaurantRatingServiceException(
                "Authenticated user account was not found or is not allowed to rate restaurants.",
                StatusCodes.Status401Unauthorized);
        }

        var restaurantExists = await _dbContext.Restaurants
            .AsNoTracking()
            .AnyAsync(
                restaurant => restaurant.Id == restaurantId
                    && restaurant.ApprovalStatus == RestaurantApprovalStatus.Approved,
                cancellationToken);

        if (!restaurantExists)
        {
            throw new RestaurantRatingServiceException(
                "Restaurant was not found or is not available for rating.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["The selected restaurant was not found or is not approved."]
                });
        }

        var rating = await _dbContext.RestaurantRatings
            .AsNoTracking()
            .Include(entity => entity.Restaurant)
            .ThenInclude(restaurant => restaurant!.Ratings)
            .FirstOrDefaultAsync(
                entity => entity.UserId == userId && entity.RestaurantId == restaurantId,
                cancellationToken);

        return rating is null ? null : MapRating(rating);
    }

    public async Task<RestaurantRatingSummaryDto> GetRatingSummaryAsync(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        if (restaurantId == Guid.Empty)
        {
            throw BuildValidationException("restaurantId", "Restaurant id is required.");
        }

        var restaurantExists = await _dbContext.Restaurants
            .AsNoTracking()
            .AnyAsync(
                restaurant => restaurant.Id == restaurantId
                    && restaurant.ApprovalStatus == RestaurantApprovalStatus.Approved,
                cancellationToken);

        if (!restaurantExists)
        {
            throw new RestaurantRatingServiceException(
                "Restaurant was not found or is not publicly available.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["The selected restaurant was not found or is not approved."]
                });
        }

        var ratingStats = await _dbContext.RestaurantRatings
            .AsNoTracking()
            .Where(rating => rating.RestaurantId == restaurantId)
            .GroupBy(rating => rating.RestaurantId)
            .Select(group => new
            {
                RestaurantId = group.Key,
                AverageRating = Math.Round(group.Average(rating => (double)rating.Stars), 2),
                TotalRatingsCount = group.Count(),
                StarCounts = group
                    .GroupBy(rating => rating.Stars)
                    .Select(starGroup => new
                    {
                        Stars = starGroup.Key,
                        Count = starGroup.Count()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (ratingStats is null)
        {
            return new RestaurantRatingSummaryDto
            {
                RestaurantId = restaurantId,
                AverageRating = 0d,
                TotalRatingsCount = 0,
                Distribution = BuildRatingDistribution(Array.Empty<(int Stars, int Count)>(), 0)
            };
        }

        return new RestaurantRatingSummaryDto
        {
            RestaurantId = ratingStats.RestaurantId,
            AverageRating = ratingStats.AverageRating,
            TotalRatingsCount = ratingStats.TotalRatingsCount,
            Distribution = BuildRatingDistribution(
                ratingStats.StarCounts.Select(item => (item.Stars, item.Count)),
                ratingStats.TotalRatingsCount)
        };
    }

    public async Task<IReadOnlyList<PublicRestaurantRatingDto>> GetPublicRatingsAsync(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        if (restaurantId == Guid.Empty)
        {
            throw BuildValidationException("restaurantId", "Restaurant id is required.");
        }

        var restaurantExists = await _dbContext.Restaurants
            .AsNoTracking()
            .AnyAsync(
                restaurant => restaurant.Id == restaurantId
                    && restaurant.ApprovalStatus == RestaurantApprovalStatus.Approved,
                cancellationToken);

        if (!restaurantExists)
        {
            throw new RestaurantRatingServiceException(
                "Restaurant was not found or is not publicly available.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["The selected restaurant was not found or is not approved."]
                });
        }

        return await _dbContext.RestaurantRatings
            .AsNoTracking()
            .Where(rating => rating.RestaurantId == restaurantId)
            .Include(rating => rating.User)
            .OrderByDescending(rating => rating.UpdatedAtUtc)
            .ThenByDescending(rating => rating.CreatedAtUtc)
            .Select(rating => new PublicRestaurantRatingDto
            {
                RatingId = rating.Id,
                RestaurantId = rating.RestaurantId,
                AverageRating = rating.Restaurant != null
                    ? Math.Round(rating.Restaurant.Ratings.Select(item => (double?)item.Stars).Average() ?? 0d, 2)
                    : 0d,
                TotalRatingsCount = rating.Restaurant != null
                    ? rating.Restaurant.Ratings.Count()
                    : 0,
                User = new PublicRatingUserDto
                {
                    DisplayName = BuildPublicDisplayName(rating.User != null ? rating.User.FullName : string.Empty)
                },
                Stars = rating.Stars,
                Comment = rating.Comment,
                CreatedAtUtc = rating.CreatedAtUtc,
                UpdatedAtUtc = rating.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    private static RestaurantRatingDto MapRating(RestaurantRating rating)
    {
        var averageRating = rating.Restaurant is null
            ? 0d
            : Math.Round(rating.Restaurant.Ratings.Select(item => (double)item.Stars).DefaultIfEmpty().Average(), 2);
        var totalRatingsCount = rating.Restaurant?.Ratings.Count ?? 0;

        return new RestaurantRatingDto
        {
            RatingId = rating.Id,
            RestaurantId = rating.RestaurantId,
            AverageRating = averageRating,
            TotalRatingsCount = totalRatingsCount,
            UserId = rating.UserId,
            Stars = rating.Stars,
            Comment = rating.Comment,
            CreatedAtUtc = rating.CreatedAtUtc,
            UpdatedAtUtc = rating.UpdatedAtUtc
        };
    }

    private static string? NormalizeComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        return comment.Trim();
    }

    private static string BuildPublicDisplayName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "Anonymous";
        }

        var parts = fullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return "Anonymous";
        }

        if (parts.Length == 1)
        {
            var firstName = parts[0];
            return firstName.Length == 1
                ? $"{firstName}."
                : $"{firstName[0]}{new string('*', Math.Min(firstName.Length - 1, 2))}";
        }

        return $"{parts[0]} {parts[^1][0]}.";
    }

    private static IReadOnlyList<RestaurantRatingDistributionItemDto> BuildRatingDistribution(
        IEnumerable<(int Stars, int Count)> starCounts,
        int totalRatingsCount)
    {
        var countsByStars = starCounts.ToDictionary(item => item.Stars, item => item.Count);
        var distribution = new List<RestaurantRatingDistributionItemDto>(5);

        for (var stars = 5; stars >= 1; stars--)
        {
            var count = countsByStars.GetValueOrDefault(stars, 0);
            var percentage = totalRatingsCount == 0
                ? 0m
                : Math.Round((decimal)count / totalRatingsCount * 100m, 2);

            distribution.Add(new RestaurantRatingDistributionItemDto
            {
                Stars = stars,
                Count = count,
                Percentage = percentage
            });
        }

        return distribution;
    }

    private static RestaurantRatingServiceException BuildValidationException(string key, string message)
    {
        return new RestaurantRatingServiceException(
            message,
            StatusCodes.Status400BadRequest,
            new Dictionary<string, string[]>
            {
                [key] = [message]
            });
    }
}
