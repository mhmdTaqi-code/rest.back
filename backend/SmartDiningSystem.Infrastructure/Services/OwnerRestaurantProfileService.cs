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

        return new OwnerRestaurantStatusDto
        {
            RestaurantId = restaurant.Id,
            RestaurantName = restaurant.Name,
            ImageUrl = restaurant.ImageUrl,
            ApprovalStatus = restaurant.ApprovalStatus.ToString(),
            RejectionReason = restaurant.RejectionReason,
            CreatedAtUtc = restaurant.CreatedAtUtc,
            ApprovedAtUtc = restaurant.ApprovedAtUtc,
            RejectedAtUtc = restaurant.RejectedAtUtc
        };
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
