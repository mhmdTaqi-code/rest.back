using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using SmartDiningSystem.Infrastructure.Data;
using SmartDiningSystem.Application.DTOs.Restaurants;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Application.Utilities;

namespace SmartDiningSystem.Infrastructure.Services;

public class AdminRestaurantService : IAdminRestaurantService
{
    private readonly AppDbContext _dbContext;

    public AdminRestaurantService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AdminPendingRestaurantDto>> GetPendingRestaurantsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Restaurants
            .AsNoTracking()
            .Where(restaurant => restaurant.ApprovalStatus == RestaurantApprovalStatus.Pending)
            .OrderBy(restaurant => restaurant.CreatedAtUtc)
            .Select(restaurant => new AdminPendingRestaurantDto
            {
                Id = restaurant.Id,
                RestaurantName = restaurant.Name,
                OwnerName = restaurant.Owner!.FullName,
                OwnerPhoneNumber = restaurant.Owner!.PhoneNumber,
                CreatedAtUtc = restaurant.CreatedAtUtc,
                AverageRating = Math.Round(restaurant.Ratings.Select(rating => (double?)rating.Stars).Average() ?? 0d, 2),
                TotalRatingsCount = restaurant.Ratings.Count()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminRestaurantDetailsDto> CreateRestaurantForOwnerAsync(
        AdminCreateRestaurantRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var owner = await _dbContext.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                account => account.Id == request.OwnerUserId && account.Role == UserRole.RestaurantOwner,
                cancellationToken);

        if (owner is null)
        {
            throw new AdminRestaurantServiceException(
                "Restaurant owner account was not found.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    [nameof(request.OwnerUserId)] = ["Select a valid restaurant owner account."]
                });
        }

        var restaurant = new Restaurant
        {
            Id = Guid.NewGuid(),
            OwnerId = request.OwnerUserId,
            Name = request.Name.Trim(),
            Description = NormalizeOptionalText(request.Description),
            ImageUrl = NormalizeOptionalText(request.ImageUrl),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address.Trim(),
            ContactPhone = NormalizePhoneNumber(request.ContactPhone),
            ApprovalStatus = RestaurantApprovalStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Restaurants.Add(restaurant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetRestaurantDetailsAsync(restaurant.Id, cancellationToken);
    }

    public async Task<AdminRestaurantDetailsDto> GetRestaurantDetailsAsync(Guid restaurantId, CancellationToken cancellationToken)
    {
        var restaurant = await _dbContext.Restaurants
            .AsNoTracking()
            .Where(entity => entity.Id == restaurantId)
            .Select(MapDetails())
            .FirstOrDefaultAsync(cancellationToken);

        if (restaurant is null)
        {
            throw new AdminRestaurantServiceException("Restaurant request not found.", StatusCodes.Status404NotFound);
        }

        return restaurant;
    }

    public async Task<AdminRestaurantDetailsDto> ApproveRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken)
    {
        var restaurant = await FindRestaurantAsync(restaurantId, cancellationToken);

        if (restaurant.ApprovalStatus != RestaurantApprovalStatus.Pending)
        {
            throw new AdminRestaurantServiceException("Only pending restaurants can be approved.");
        }

        restaurant.ApprovalStatus = RestaurantApprovalStatus.Approved;
        restaurant.ApprovedAtUtc = DateTime.UtcNow;
        restaurant.RejectionReason = null;
        restaurant.RejectedAtUtc = null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetRestaurantDetailsAsync(restaurantId, cancellationToken);
    }

    public async Task<AdminRestaurantDetailsDto> RejectRestaurantAsync(
        Guid restaurantId,
        string rejectionReason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            throw new AdminRestaurantServiceException(
                "Validation failed.",
                errors: new Dictionary<string, string[]>
                {
                    [nameof(AdminRejectRestaurantRequestDto.RejectionReason)] = ["Rejection reason is required."]
                });
        }

        var restaurant = await FindRestaurantAsync(restaurantId, cancellationToken);

        if (restaurant.ApprovalStatus != RestaurantApprovalStatus.Pending)
        {
            throw new AdminRestaurantServiceException("Only pending restaurants can be rejected.");
        }

        restaurant.ApprovalStatus = RestaurantApprovalStatus.Rejected;
        restaurant.RejectionReason = rejectionReason.Trim();
        restaurant.RejectedAtUtc = DateTime.UtcNow;
        restaurant.ApprovedAtUtc = null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetRestaurantDetailsAsync(restaurantId, cancellationToken);
    }

    private async Task<Restaurant> FindRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken)
    {
        var restaurant = await _dbContext.Restaurants
            .FirstOrDefaultAsync(entity => entity.Id == restaurantId, cancellationToken);

        if (restaurant is null)
        {
            throw new AdminRestaurantServiceException("Restaurant request not found.", StatusCodes.Status404NotFound);
        }

        return restaurant;
    }

    private static System.Linq.Expressions.Expression<Func<Restaurant, AdminRestaurantDetailsDto>> MapDetails()
    {
        return entity => new AdminRestaurantDetailsDto
        {
            Id = entity.Id,
            RestaurantName = entity.Name,
            RestaurantDescription = entity.Description,
            ImageUrl = entity.ImageUrl,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            RestaurantAddress = entity.Address,
            RestaurantPhoneNumber = entity.ContactPhone,
            OwnerName = entity.Owner!.FullName,
            OwnerPhoneNumber = entity.Owner!.PhoneNumber,
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
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizePhoneNumber(string rawPhoneNumber)
    {
        if (!IraqiPhoneNumberHelper.TryNormalize(rawPhoneNumber, out var normalizedPhoneNumber))
        {
            throw new AdminRestaurantServiceException(
                "Restaurant phone number must be a valid Iraqi mobile number.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    [nameof(AdminCreateRestaurantRequestDto.ContactPhone)] = ["Restaurant phone number must be a valid Iraqi mobile number."]
                });
        }

        return normalizedPhoneNumber;
    }
}
