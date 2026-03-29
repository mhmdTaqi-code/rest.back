using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.Restaurants;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class OwnerRestaurantProfileService : IOwnerRestaurantProfileService
{
    private readonly AppDbContext _dbContext;

    public OwnerRestaurantProfileService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OwnerRestaurantStatusDto> UpdateRestaurantImageAsync(
        Guid ownerId,
        UpdateRestaurantImageRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var restaurant = await _dbContext.Restaurants
            .FirstOrDefaultAsync(entity => entity.OwnerId == ownerId, cancellationToken);

        if (restaurant is null)
        {
            throw new OwnerRestaurantProfileServiceException(
                "Restaurant was not found for this owner.",
                StatusCodes.Status404NotFound);
        }

        restaurant.ImageUrl = NormalizeImageUrl(request.ImageUrl);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildOwnerRestaurantStatusDtoAsync(restaurant.Id, cancellationToken);
    }

    public async Task<OwnerRestaurantStatusDto> UpdateRestaurantLocationAsync(
        Guid ownerId,
        UpdateRestaurantLocationRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var restaurant = await _dbContext.Restaurants
            .FirstOrDefaultAsync(entity => entity.OwnerId == ownerId, cancellationToken);

        if (restaurant is null)
        {
            throw new OwnerRestaurantProfileServiceException(
                "Restaurant was not found for this owner.",
                StatusCodes.Status404NotFound);
        }

        restaurant.Latitude = request.Latitude;
        restaurant.Longitude = request.Longitude;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildOwnerRestaurantStatusDtoAsync(restaurant.Id, cancellationToken);
    }

    private async Task<OwnerRestaurantStatusDto> BuildOwnerRestaurantStatusDtoAsync(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Restaurants
            .AsNoTracking()
            .Where(entity => entity.Id == restaurantId)
            .Select(entity => new OwnerRestaurantStatusDto
            {
                RestaurantId = entity.Id,
                RestaurantName = entity.Name,
                ImageUrl = entity.ImageUrl,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                ApprovalStatus = entity.ApprovalStatus.ToString(),
                RejectionReason = entity.RejectionReason,
                CreatedAtUtc = entity.CreatedAtUtc,
                ApprovedAtUtc = entity.ApprovedAtUtc,
                RejectedAtUtc = entity.RejectedAtUtc,
                AverageRating = Math.Round(entity.Ratings.Select(rating => (double?)rating.Stars).Average() ?? 0d, 2),
                TotalRatingsCount = entity.Ratings.Count()
            })
            .FirstAsync(cancellationToken);
    }

    private static string? NormalizeImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        return imageUrl.Trim();
    }
}
