using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.Restaurants;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Application.Utilities;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class OwnerRestaurantProfileService : IOwnerRestaurantProfileService
{
    private readonly AppDbContext _dbContext;

    public OwnerRestaurantProfileService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OwnerRestaurantStatusDto>> GetRestaurantsAsync(
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Restaurants
            .AsNoTracking()
            .Where(entity => entity.OwnerId == ownerId)
            .OrderBy(entity => entity.CreatedAtUtc)
            .Select(MapRestaurant())
            .ToListAsync(cancellationToken);
    }

    public async Task<OwnerRestaurantStatusDto> CreateRestaurantAsync(
        Guid ownerId,
        CreateOwnerRestaurantRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ownerExists = await _dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(account => account.Id == ownerId, cancellationToken);

        if (!ownerExists)
        {
            throw new OwnerRestaurantProfileServiceException(
                "Restaurant owner account was not found.",
                StatusCodes.Status404NotFound);
        }

        var restaurant = new Domain.Entities.Restaurant
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = request.Name.Trim(),
            Description = NormalizeOptionalText(request.Description),
            Address = request.Address.Trim(),
            ContactPhone = NormalizePhoneNumber(request.ContactPhone),
            ImageUrl = NormalizeOptionalText(request.ImageUrl),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ApprovalStatus = Domain.Enums.RestaurantApprovalStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Restaurants.Add(restaurant);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildOwnerRestaurantStatusDtoAsync(restaurant.Id, cancellationToken);
    }

    public async Task<OwnerRestaurantStatusDto> GetRestaurantAsync(
        Guid ownerId,
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        await EnsureOwnedRestaurantExistsAsync(ownerId, restaurantId, cancellationToken);
        return await BuildOwnerRestaurantStatusDtoAsync(restaurantId, cancellationToken);
    }

    public async Task<OwnerRestaurantStatusDto> UpdateRestaurantAsync(
        Guid ownerId,
        Guid restaurantId,
        UpdateOwnerRestaurantRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var restaurant = await _dbContext.Restaurants
            .FirstOrDefaultAsync(entity => entity.Id == restaurantId && entity.OwnerId == ownerId, cancellationToken);

        if (restaurant is null)
        {
            throw new OwnerRestaurantProfileServiceException(
                "Restaurant was not found for this owner.",
                StatusCodes.Status404NotFound);
        }

        if (request.Name is not null)
        {
            restaurant.Name = request.Name.Trim();
        }

        if (request.Description is not null)
        {
            restaurant.Description = NormalizeOptionalText(request.Description);
        }

        if (request.Address is not null)
        {
            restaurant.Address = request.Address.Trim();
        }

        if (request.ContactPhone is not null)
        {
            restaurant.ContactPhone = NormalizePhoneNumber(request.ContactPhone);
        }

        if (request.ImageUrl is not null)
        {
            restaurant.ImageUrl = NormalizeOptionalText(request.ImageUrl);
        }

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            restaurant.Latitude = request.Latitude.Value;
            restaurant.Longitude = request.Longitude.Value;
        }

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
            .Select(MapRestaurant())
            .FirstAsync(cancellationToken);
    }

    private async Task EnsureOwnedRestaurantExistsAsync(
        Guid ownerId,
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Restaurants
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == restaurantId && entity.OwnerId == ownerId, cancellationToken);

        if (!exists)
        {
            throw new OwnerRestaurantProfileServiceException(
                "Restaurant was not found for this owner.",
                StatusCodes.Status404NotFound);
        }
    }

    private static System.Linq.Expressions.Expression<Func<Domain.Entities.Restaurant, OwnerRestaurantStatusDto>> MapRestaurant()
    {
        return entity => new OwnerRestaurantStatusDto
        {
            RestaurantId = entity.Id,
            RestaurantName = entity.Name,
            Description = entity.Description,
            Address = entity.Address,
            ContactPhone = entity.ContactPhone,
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
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string NormalizePhoneNumber(string rawPhoneNumber)
    {
        if (!IraqiPhoneNumberHelper.TryNormalize(rawPhoneNumber, out var normalizedPhoneNumber))
        {
            throw new OwnerRestaurantProfileServiceException(
                "Restaurant phone number must be a valid Iraqi mobile number.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["contactPhone"] = ["Restaurant phone number must be a valid Iraqi mobile number."]
                });
        }

        return normalizedPhoneNumber;
    }
}
