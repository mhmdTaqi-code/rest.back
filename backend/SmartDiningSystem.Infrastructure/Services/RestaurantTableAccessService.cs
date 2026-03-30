using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.TableOrdering;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class RestaurantTableAccessService : IRestaurantTableAccessService
{
    private readonly AppDbContext _dbContext;

    public RestaurantTableAccessService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ResolvedRestaurantTableDto> ResolveTableAsync(
        Guid restaurantId,
        Guid tableId,
        CancellationToken cancellationToken)
    {
        ValidateRouteContext(restaurantId, tableId);

        var table = await _dbContext.RestaurantTables
            .AsNoTracking()
            .Where(entity => entity.Id == tableId)
            .Select(entity => new
            {
                entity.Id,
                entity.TableNumber,
                entity.ImageUrl,
                entity.TableToken,
                entity.IsActive,
                entity.RestaurantId,
                RestaurantName = entity.Restaurant != null ? entity.Restaurant.Name : null,
                AverageRating = entity.Restaurant != null
                    ? Math.Round(entity.Restaurant.Ratings.Select(rating => (double?)rating.Stars).Average() ?? 0d, 2)
                    : 0d,
                TotalRatingsCount = entity.Restaurant != null
                    ? entity.Restaurant.Ratings.Count()
                    : 0,
                ApprovalStatus = entity.Restaurant != null
                    ? entity.Restaurant.ApprovalStatus
                    : (RestaurantApprovalStatus?)null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (table is null)
        {
            throw new TableOrderingServiceException(
                "Restaurant table was not found.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["tableId"] = ["The selected restaurant table was not found."]
                });
        }

        if (table.RestaurantId != restaurantId)
        {
            throw new TableOrderingServiceException(
                "The selected table does not belong to the specified restaurant.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["The selected table does not belong to the specified restaurant."],
                    ["tableId"] = ["The selected table does not belong to the specified restaurant."]
                });
        }

        if (!table.IsActive)
        {
            throw new TableOrderingServiceException(
                "This table is not currently active.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["tableId"] = ["The selected restaurant table is inactive."]
                });
        }

        if (table.ApprovalStatus is null || string.IsNullOrWhiteSpace(table.RestaurantName))
        {
            throw new TableOrderingServiceException(
                "This restaurant table is not linked to a valid restaurant.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["The selected restaurant was not found for this table."],
                    ["tableId"] = ["The table is not available for ordering."]
                });
        }

        if (table.ApprovalStatus != RestaurantApprovalStatus.Approved)
        {
            throw new TableOrderingServiceException(
                "This restaurant is not available for public table ordering.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["The selected restaurant is not available for ordering."],
                    ["tableId"] = ["The table is not available for ordering."]
                });
        }

        return new ResolvedRestaurantTableDto
        {
            RestaurantId = table.RestaurantId,
            RestaurantName = table.RestaurantName,
            AverageRating = table.AverageRating,
            TotalRatingsCount = table.TotalRatingsCount,
            RestaurantTableId = table.Id,
            TableNumber = table.TableNumber,
            ImageUrl = table.ImageUrl,
            TableDisplayName = $"Table {table.TableNumber}",
            TableToken = table.TableToken,
            RequiresAuthenticationForOrdering = true
        };
    }

    private static void ValidateRouteContext(Guid restaurantId, Guid tableId)
    {
        Dictionary<string, string[]>? errors = null;

        if (restaurantId == Guid.Empty)
        {
            errors = new Dictionary<string, string[]>
            {
                ["restaurantId"] = ["Restaurant id is required."]
            };
        }

        if (tableId == Guid.Empty)
        {
            errors ??= new Dictionary<string, string[]>();
            errors["tableId"] = ["Table id is required."];
        }

        if (errors is not null)
        {
            throw new TableOrderingServiceException(
                "Restaurant and table identifiers are required.",
                StatusCodes.Status400BadRequest,
                errors);
        }
    }
}
