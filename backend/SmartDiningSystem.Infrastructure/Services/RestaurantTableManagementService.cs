using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.RestaurantTables;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class RestaurantTableManagementService : IRestaurantTableManagementService
{
    private readonly AppDbContext _dbContext;

    public RestaurantTableManagementService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RestaurantTableDto>> GetOwnerTablesAsync(Guid ownerId, Guid restaurantId, CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);

        return await _dbContext.RestaurantTables
            .AsNoTracking()
            .Where(table => table.RestaurantId == restaurant.Id)
            .OrderBy(table => table.TableNumber)
            .Select(MapTable(
                restaurant.Name,
                CalculateAverageRating(restaurant),
                restaurant.Ratings.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<RestaurantTableDto> CreateTableAsync(
        Guid ownerId,
        Guid restaurantId,
        CreateRestaurantTableRequestDto request,
        CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);

        var tableNumberExists = await _dbContext.RestaurantTables
            .AnyAsync(
                table => table.RestaurantId == restaurant.Id && table.TableNumber == request.TableNumber,
                cancellationToken);

        if (tableNumberExists)
        {
            throw new RestaurantTableManagementServiceException(
                "A table with this number already exists for your restaurant.",
                StatusCodes.Status409Conflict,
                new Dictionary<string, string[]>
                {
                    [nameof(request.TableNumber)] = ["Table number must be unique within the restaurant."]
                });
        }

        var nowUtc = DateTime.UtcNow;
        var table = new RestaurantTable
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurant.Id,
            TableNumber = request.TableNumber,
            ImageUrl = NormalizeImageUrl(request.ImageUrl),
            TableToken = await GenerateUniqueTableTokenAsync(cancellationToken),
            IsActive = true,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

        _dbContext.RestaurantTables.Add(table);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapTableValue(
            table,
            restaurant.Name,
            CalculateAverageRating(restaurant),
            restaurant.Ratings.Count);
    }

    public async Task<IReadOnlyList<RestaurantTableDto>> BulkCreateTablesAsync(
        Guid ownerId,
        Guid restaurantId,
        BulkCreateRestaurantTablesRequestDto request,
        CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);
        var nowUtc = DateTime.UtcNow;

        var nextTableNumber = await _dbContext.RestaurantTables
            .Where(table => table.RestaurantId == restaurant.Id)
            .Select(table => (int?)table.TableNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var createdTables = new List<RestaurantTable>(request.TableCount);
        for (var index = 0; index < request.TableCount; index++)
        {
            nextTableNumber++;
            createdTables.Add(new RestaurantTable
            {
                Id = Guid.NewGuid(),
                RestaurantId = restaurant.Id,
                TableNumber = nextTableNumber,
                TableToken = await GenerateUniqueTableTokenAsync(cancellationToken),
                IsActive = true,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            });
        }

        _dbContext.RestaurantTables.AddRange(createdTables);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return createdTables
            .Select(table => MapTableValue(
                table,
                restaurant.Name,
                CalculateAverageRating(restaurant),
                restaurant.Ratings.Count))
            .ToList();
    }

    public async Task<RestaurantTableDto> UpdateTableStatusAsync(
        Guid ownerId,
        Guid restaurantId,
        Guid tableId,
        UpdateRestaurantTableStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);
        var table = await GetOwnedTableAsync(restaurant.Id, tableId, cancellationToken);

        table.IsActive = request.IsActive;
        table.ImageUrl = NormalizeImageUrl(request.ImageUrl);
        table.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapTableValue(
            table,
            restaurant.Name,
            CalculateAverageRating(restaurant),
            restaurant.Ratings.Count);
    }

    public async Task DeleteTableAsync(Guid ownerId, Guid restaurantId, Guid tableId, CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);
        var table = await GetOwnedTableAsync(restaurant.Id, tableId, cancellationToken);

        var hasOrders = await _dbContext.Orders
            .AnyAsync(order => order.RestaurantTableId == tableId, cancellationToken);
        if (hasOrders)
        {
            throw new RestaurantTableManagementServiceException(
                "This table cannot be deleted because it is referenced by existing orders.",
                StatusCodes.Status409Conflict);
        }

        var hasBookings = await _dbContext.Bookings
            .AnyAsync(booking => booking.RestaurantTableId == tableId, cancellationToken);
        if (hasBookings)
        {
            throw new RestaurantTableManagementServiceException(
                "This table cannot be deleted because it is referenced by existing bookings.",
                StatusCodes.Status409Conflict);
        }

        var hasSessions = await _dbContext.TableSessions
            .AnyAsync(session => session.RestaurantTableId == tableId, cancellationToken);
        if (hasSessions)
        {
            throw new RestaurantTableManagementServiceException(
                "This table cannot be deleted because it is referenced by existing table sessions.",
                StatusCodes.Status409Conflict);
        }

        var hasCarts = await _dbContext.TableCarts
            .AnyAsync(cart => cart.RestaurantTableId == tableId, cancellationToken);
        if (hasCarts)
        {
            throw new RestaurantTableManagementServiceException(
                "This table cannot be deleted because it is referenced by active carts.",
                StatusCodes.Status409Conflict);
        }

        _dbContext.RestaurantTables.Remove(table);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Restaurant> GetApprovedOwnerRestaurantAsync(Guid ownerId, Guid restaurantId, CancellationToken cancellationToken)
    {
        var restaurant = await _dbContext.Restaurants
            .Include(entity => entity.Ratings)
            .FirstOrDefaultAsync(entity => entity.OwnerId == ownerId && entity.Id == restaurantId, cancellationToken);

        if (restaurant is null)
        {
            throw new RestaurantTableManagementServiceException(
                "Restaurant was not found for this owner.",
                StatusCodes.Status404NotFound);
        }

        if (restaurant.ApprovalStatus != RestaurantApprovalStatus.Approved)
        {
            throw new RestaurantTableManagementServiceException(
                "Only approved restaurant owners can manage restaurant tables.",
                StatusCodes.Status403Forbidden,
                new Dictionary<string, string[]>
                {
                    ["approvalStatus"] = [restaurant.ApprovalStatus.ToString()]
                });
        }

        return restaurant;
    }

    private async Task<RestaurantTable> GetOwnedTableAsync(Guid restaurantId, Guid tableId, CancellationToken cancellationToken)
    {
        var table = await _dbContext.RestaurantTables
            .FirstOrDefaultAsync(
                entity => entity.Id == tableId && entity.RestaurantId == restaurantId,
                cancellationToken);

        if (table is null)
        {
            throw new RestaurantTableManagementServiceException(
                "Restaurant table was not found for this restaurant.",
                StatusCodes.Status404NotFound);
        }

        return table;
    }

    private async Task<string> GenerateUniqueTableTokenAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(12);
            var candidate = WebEncoders.Base64UrlEncode(tokenBytes);
            var exists = await _dbContext.RestaurantTables
                .AnyAsync(table => table.TableToken == candidate, cancellationToken);

            if (!exists)
            {
                return candidate;
            }
        }
    }

    private static System.Linq.Expressions.Expression<Func<RestaurantTable, RestaurantTableDto>> MapTable(
        string restaurantName,
        double averageRating,
        int totalRatingsCount)
    {
        return table => new RestaurantTableDto
        {
            Id = table.Id,
            RestaurantId = table.RestaurantId,
            RestaurantName = restaurantName,
            AverageRating = averageRating,
            TotalRatingsCount = totalRatingsCount,
            TableNumber = table.TableNumber,
            ImageUrl = table.ImageUrl,
            TableToken = table.TableToken,
            IsActive = table.IsActive,
            CreatedAtUtc = table.CreatedAtUtc,
            UpdatedAtUtc = table.UpdatedAtUtc
        };
    }

    private static RestaurantTableDto MapTableValue(
        RestaurantTable table,
        string restaurantName,
        double averageRating,
        int totalRatingsCount)
    {
        return new RestaurantTableDto
        {
            Id = table.Id,
            RestaurantId = table.RestaurantId,
            RestaurantName = restaurantName,
            AverageRating = averageRating,
            TotalRatingsCount = totalRatingsCount,
            TableNumber = table.TableNumber,
            ImageUrl = table.ImageUrl,
            TableToken = table.TableToken,
            IsActive = table.IsActive,
            CreatedAtUtc = table.CreatedAtUtc,
            UpdatedAtUtc = table.UpdatedAtUtc
        };
    }

    private static string? NormalizeImageUrl(string? imageUrl)
    {
        return string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
    }

    private static double CalculateAverageRating(Restaurant restaurant)
    {
        return Math.Round(restaurant.Ratings.Select(rating => (double)rating.Stars).DefaultIfEmpty().Average(), 2);
    }
}
