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
            .AnyAsync(restaurant => restaurant.Id == restaurantId, cancellationToken);

        if (!restaurantExists)
        {
            throw new RestaurantRatingServiceException(
                "Restaurant was not found.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["The selected restaurant was not found."]
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
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            };

            _dbContext.RestaurantRatings.Add(rating);
        }
        else
        {
            rating.Stars = request.Stars;
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
            .AnyAsync(restaurant => restaurant.Id == restaurantId, cancellationToken);

        if (!restaurantExists)
        {
            throw new RestaurantRatingServiceException(
                "Restaurant was not found.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["The selected restaurant was not found."]
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
            .AnyAsync(restaurant => restaurant.Id == restaurantId, cancellationToken);

        if (!restaurantExists)
        {
            throw new RestaurantRatingServiceException(
                "Restaurant was not found.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["The selected restaurant was not found."]
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

    private static RestaurantRatingDto MapRating(RestaurantRating rating)
    {
        return new RestaurantRatingDto
        {
            RestaurantId = rating.RestaurantId,
            UserId = rating.UserId,
            Stars = rating.Stars,
            CreatedAtUtc = rating.CreatedAtUtc,
            UpdatedAtUtc = rating.UpdatedAtUtc
        };
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
