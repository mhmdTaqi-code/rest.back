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

        var hasOrderedFromRestaurant = await _dbContext.Orders
            .AsNoTracking()
            .AnyAsync(
                order => order.UserId == userId && order.RestaurantId == restaurantId,
                cancellationToken);

        if (!hasOrderedFromRestaurant)
        {
            throw new RestaurantRatingServiceException(
                "You can only rate restaurants you have ordered from.",
                StatusCodes.Status403Forbidden,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["Place at least one order from this restaurant before rating it."]
                });
        }

        var rating = await _dbContext.RestaurantRatings
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

        var summary = await _dbContext.RestaurantRatings
            .AsNoTracking()
            .Where(rating => rating.RestaurantId == restaurantId)
            .GroupBy(rating => rating.RestaurantId)
            .Select(group => new RestaurantRatingSummaryDto
            {
                RestaurantId = group.Key,
                AverageRating = decimal.Round(group.Average(rating => (decimal)rating.Stars), 2),
                TotalRatingsCount = group.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return summary ?? new RestaurantRatingSummaryDto
        {
            RestaurantId = restaurantId,
            AverageRating = 0m,
            TotalRatingsCount = 0
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
        return new RestaurantRatingDto
        {
            RatingId = rating.Id,
            RestaurantId = rating.RestaurantId,
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
