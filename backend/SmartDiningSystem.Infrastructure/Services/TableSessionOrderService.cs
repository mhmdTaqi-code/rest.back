using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.TableOrdering;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class TableSessionOrderService : ITableSessionOrderService
{
    private readonly AppDbContext _dbContext;

    public TableSessionOrderService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SubmittedTableOrderResponseDto> SubmitOrderAsync(
        Guid userId,
        Guid sessionId,
        SubmitTableOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (sessionId == Guid.Empty)
        {
            throw BuildValidationError("Session id is required.", "sessionId", "Session id is required.");
        }

        var session = await _dbContext.TableSessions
            .AsNoTracking()
            .Include(entity => entity.RestaurantTable)
            .FirstOrDefaultAsync(entity => entity.Id == sessionId, cancellationToken);

        if (session is null || session.Status != TableSessionStatus.Open)
        {
            throw new BookingFlowServiceException(
                "Active table session was not found.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["sessionId"] = ["The selected table session was not found."]
                });
        }

        if (session.UserId.HasValue && session.UserId.Value != userId)
        {
            throw new BookingFlowServiceException(
                "This table session does not belong to the current user.",
                StatusCodes.Status403Forbidden,
                new Dictionary<string, string[]>
                {
                    ["sessionId"] = ["You do not have access to this table session."]
                });
        }

        if (request.Items.Count == 0)
        {
            throw BuildValidationError(
                "Order items are required.",
                "items",
                "Add at least one item before submitting an order.");
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            throw BuildValidationError(
                "Order contains invalid quantities.",
                "items",
                "Each selected item must have a quantity greater than zero.");
        }

        if (request.Items.Select(item => item.MenuItemId).Distinct().Count() != request.Items.Count)
        {
            throw BuildValidationError(
                "Order contains duplicate menu items.",
                "items",
                "Each menu item may only appear once in the order.");
        }

        var menuItemIds = request.Items
            .Select(item => item.MenuItemId)
            .Distinct()
            .ToList();

        var menuItems = await _dbContext.MenuItems
            .AsNoTracking()
            .Include(item => item.MenuCategory)
            .Where(item => menuItemIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        var menuItemsById = menuItems.ToDictionary(item => item.Id);

        foreach (var requestItem in request.Items)
        {
            if (!menuItemsById.TryGetValue(requestItem.MenuItemId, out var menuItem))
            {
                throw BuildValidationError(
                    "Order contains an invalid menu item.",
                    "items",
                    "One or more selected menu items were not found.");
            }

            if (menuItem.RestaurantId != session.RestaurantId ||
                menuItem.MenuCategory is null ||
                menuItem.MenuCategory.RestaurantId != session.RestaurantId)
            {
                throw BuildValidationError(
                    "Order contains menu items from a different restaurant.",
                    "items",
                    "All selected items must belong to the same restaurant as the active table session.");
            }

            if (!menuItem.IsAvailable || !menuItem.MenuCategory.IsActive)
            {
                throw BuildValidationError(
                    "Order contains unavailable menu items.",
                    "items",
                    "Remove unavailable items before submitting the order.");
            }
        }

        var nowUtc = DateTime.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RestaurantId = session.RestaurantId,
            RestaurantTableId = session.RestaurantTableId,
            TableSessionId = session.Id,
            Status = OrderStatus.OrderReceived,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            OrderItems = request.Items
                .Select(item => new OrderItem
                {
                    Id = Guid.NewGuid(),
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    UnitPrice = menuItemsById[item.MenuItemId].Price
                })
                .ToList()
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var totalAmount = order.OrderItems.Sum(item => item.UnitPrice * item.Quantity);

        return new SubmittedTableOrderResponseDto
        {
            OrderId = order.Id,
            UserId = order.UserId,
            RestaurantId = order.RestaurantId,
            RestaurantTableId = order.RestaurantTableId,
            TableNumber = session.RestaurantTable?.TableNumber ?? 0,
            OrderName = $"Table {session.RestaurantTable?.TableNumber ?? 0}",
            Status = order.Status.ToString(),
            ItemCount = order.OrderItems.Sum(item => item.Quantity),
            TotalAmount = totalAmount,
            CreatedAtUtc = order.CreatedAtUtc
        };
    }

    private static BookingFlowServiceException BuildValidationError(string message, string key, string error)
    {
        return new BookingFlowServiceException(
            message,
            StatusCodes.Status400BadRequest,
            new Dictionary<string, string[]>
            {
                [key] = [error]
            });
    }
}
