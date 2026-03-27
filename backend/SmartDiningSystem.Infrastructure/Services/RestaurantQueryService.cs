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
                ContactPhone = restaurant.ContactPhone
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
                RejectedAtUtc = entity.RejectedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (restaurant is null)
        {
            throw new AuthServiceException("Restaurant was not found for this owner.", StatusCodes.Status404NotFound);
        }

        return restaurant;
    }
}
