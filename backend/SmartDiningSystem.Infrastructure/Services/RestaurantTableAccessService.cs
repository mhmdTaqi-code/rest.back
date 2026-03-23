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

    public async Task<ResolvedRestaurantTableDto> ResolveTableAsync(string tableToken, CancellationToken cancellationToken)
    {
        var normalizedToken = NormalizeTableToken(tableToken);

        var table = await _dbContext.RestaurantTables
            .AsNoTracking()
            .Where(entity => entity.TableToken == normalizedToken)
            .Select(entity => new
            {
                entity.Id,
                entity.TableNumber,
                entity.TableToken,
                entity.IsActive,
                entity.RestaurantId,
                RestaurantName = entity.Restaurant != null ? entity.Restaurant.Name : null,
                ApprovalStatus = entity.Restaurant != null
                    ? entity.Restaurant.ApprovalStatus
                    : (RestaurantApprovalStatus?)null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (table is null)
        {
            throw new TableOrderingServiceException(
                "Table token is invalid.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["tableToken"] = ["The provided table token was not found."]
                });
        }

        if (!table.IsActive)
        {
            throw new TableOrderingServiceException(
                "This table is not currently active.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["tableToken"] = ["The selected restaurant table is inactive."]
                });
        }

        if (table.ApprovalStatus is null || string.IsNullOrWhiteSpace(table.RestaurantName))
        {
            throw new TableOrderingServiceException(
                "This restaurant table is not linked to a valid restaurant.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["tableToken"] = ["The table is not available for ordering."]
                });
        }

        if (table.ApprovalStatus != RestaurantApprovalStatus.Approved)
        {
            throw new TableOrderingServiceException(
                "This restaurant is not available for public table ordering.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["tableToken"] = ["The table is not available for ordering."]
                });
        }

        return new ResolvedRestaurantTableDto
        {
            RestaurantId = table.RestaurantId,
            RestaurantName = table.RestaurantName,
            RestaurantTableId = table.Id,
            TableNumber = table.TableNumber,
            TableDisplayName = $"Table {table.TableNumber}",
            TableToken = table.TableToken,
            RequiresAuthenticationForOrdering = true
        };
    }

    private static string NormalizeTableToken(string tableToken)
    {
        if (string.IsNullOrWhiteSpace(tableToken))
        {
            throw new TableOrderingServiceException(
                "Table token is required.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["tableToken"] = ["Table token is required."]
                });
        }

        return tableToken.Trim();
    }
}
