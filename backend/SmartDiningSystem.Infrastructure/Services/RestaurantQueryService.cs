using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Infrastructure.Data;
using SmartDiningSystem.Application.DTOs.Restaurants;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Infrastructure.Services;

public class RestaurantQueryService : IRestaurantQueryService
{
    private readonly AppDbContext _dbContext;

    public RestaurantQueryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PublicRestaurantSummaryDto>> GetPublicRestaurantsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Restaurants
            .AsNoTracking()
            .Where(restaurant => restaurant.ApprovalStatus == RestaurantApprovalStatus.Approved)
            .OrderBy(restaurant => restaurant.Name)
            .Select(restaurant => new PublicRestaurantSummaryDto
            {
                Id = restaurant.Id,
                Name = restaurant.Name,
                Description = restaurant.Description,
                ImageUrl = restaurant.ImageUrl,
                Latitude = restaurant.Latitude,
                Longitude = restaurant.Longitude,
                Address = restaurant.Address,
                ContactPhone = restaurant.ContactPhone,
                AverageRating = Math.Round(restaurant.Ratings.Select(rating => (double?)rating.Stars).Average() ?? 0d, 2),
                TotalRatingsCount = restaurant.Ratings.Count()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PublicRestaurantTableDto>> GetTablesByRestaurantIdAsync(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        await EnsureApprovedRestaurantExistsAsync(restaurantId, cancellationToken);

        return await _dbContext.RestaurantTables
            .AsNoTracking()
            .Where(table => table.RestaurantId == restaurantId && table.IsActive)
            .OrderBy(table => table.TableNumber)
            .Select(table => new PublicRestaurantTableDto
            {
                TableId = table.Id,
                TableNumber = table.TableNumber,
                IsActive = table.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PublicRestaurantMenuItemDto>> GetMenuByRestaurantIdAsync(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        await EnsureApprovedRestaurantExistsAsync(restaurantId, cancellationToken);

        return await _dbContext.MenuItems
            .AsNoTracking()
            .Where(item => item.RestaurantId == restaurantId && item.IsAvailable)
            .OrderBy(item => item.MenuCategory != null ? item.MenuCategory.DisplayOrder : int.MaxValue)
            .ThenBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Select(item => new PublicRestaurantMenuItemDto
            {
                MenuItemId = item.Id,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                ImageUrl = string.IsNullOrWhiteSpace(item.ImageUrl) ? null : item.ImageUrl,
                CategoryName = item.MenuCategory != null ? item.MenuCategory.Name : null,
                IsAvailable = item.IsAvailable
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OwnerRestaurantStatusDto> GetOwnerRestaurantStatusAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var restaurant = await _dbContext.Restaurants
            .AsNoTracking()
            .Where(entity => entity.OwnerId == ownerId)
            .OrderBy(entity => entity.CreatedAtUtc)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (restaurant is null)
        {
            throw new AuthServiceException("Restaurant was not found for this owner.", StatusCodes.Status404NotFound);
        }

        return restaurant;
    }

    private static RestaurantQueryServiceException BuildValidationException(string key, string message)
    {
        return new RestaurantQueryServiceException(
            message,
            StatusCodes.Status400BadRequest,
            new Dictionary<string, string[]>
            {
                [key] = [message]
            });
    }

    private async Task EnsureApprovedRestaurantExistsAsync(Guid restaurantId, CancellationToken cancellationToken)
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
            throw new RestaurantQueryServiceException(
                "Restaurant was not found or is not publicly available.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["The selected restaurant was not found or is not approved."]
                });
        }
    }
}
