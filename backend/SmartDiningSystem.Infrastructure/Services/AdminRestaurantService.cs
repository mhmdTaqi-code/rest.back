using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using SmartDiningSystem.Infrastructure.Data;
using SmartDiningSystem.Application.DTOs.Restaurants;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Application.Services.Interfaces;

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
                CreatedAtUtc = restaurant.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
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
            RestaurantAddress = entity.Address,
            RestaurantPhoneNumber = entity.ContactPhone,
            OwnerName = entity.Owner!.FullName,
            OwnerEmail = entity.Owner!.Email,
            OwnerPhoneNumber = entity.Owner!.PhoneNumber,
            ApprovalStatus = entity.ApprovalStatus.ToString(),
            RejectionReason = entity.RejectionReason,
            CreatedAtUtc = entity.CreatedAtUtc,
            ApprovedAtUtc = entity.ApprovedAtUtc,
            RejectedAtUtc = entity.RejectedAtUtc
        };
    }
}
