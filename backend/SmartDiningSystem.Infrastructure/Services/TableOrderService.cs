using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.TableOrdering;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class TableOrderService : ITableOrderService
{
    private readonly AppDbContext _dbContext;
    private readonly IRestaurantTableAccessService _restaurantTableAccessService;

    public TableOrderService(
        AppDbContext dbContext,
        IRestaurantTableAccessService restaurantTableAccessService)
    {
        _dbContext = dbContext;
        _restaurantTableAccessService = restaurantTableAccessService;
    }

    public async Task<SubmittedTableOrderResponseDto> SubmitOrderAsync(
        Guid userId,
        Guid restaurantId,
        Guid tableId,
        SubmitTableOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        var table = await _restaurantTableAccessService.ResolveTableAsync(restaurantId, tableId, cancellationToken);

        var userExists = await _dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId && user.IsActive, cancellationToken);

        if (!userExists)
        {
            throw new TableOrderingServiceException(
                "Authenticated user account was not found.",
                StatusCodes.Status401Unauthorized);
        }

        if (request.Items.Count == 0)
        {
            throw new TableOrderingServiceException(
                "Order items are required.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["items"] = ["Add at least one item before submitting an order."]
                });
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            throw new TableOrderingServiceException(
                "Order contains invalid quantities.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["items"] = ["Each selected item must have a quantity greater than zero."]
                });
        }

        if (request.Items.Select(item => item.MenuItemId).Distinct().Count() != request.Items.Count)
        {
            throw new TableOrderingServiceException(
                "Order contains duplicate menu items.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["items"] = ["Each menu item may only appear once in the order."]
                });
        }

        var requestedMenuItemIds = request.Items
            .Select(item => item.MenuItemId)
            .Distinct()
            .ToList();

        var menuItems = await _dbContext.MenuItems
            .AsNoTracking()
            .Include(item => item.MenuCategory)
            .Where(item => requestedMenuItemIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        var menuItemsById = menuItems.ToDictionary(item => item.Id);

        foreach (var requestItem in request.Items)
        {
            if (!menuItemsById.TryGetValue(requestItem.MenuItemId, out var menuItem))
            {
                throw new TableOrderingServiceException(
                    "Order contains an invalid menu item.",
                    StatusCodes.Status400BadRequest,
                    new Dictionary<string, string[]>
                    {
                        ["items"] = ["One or more selected menu items were not found."]
                    });
            }

            if (menuItem.RestaurantId != table.RestaurantId ||
                menuItem.MenuCategory is null ||
                menuItem.MenuCategory.RestaurantId != table.RestaurantId)
            {
                throw new TableOrderingServiceException(
                    "Order contains menu items from a different restaurant.",
                    StatusCodes.Status400BadRequest,
                    new Dictionary<string, string[]>
                    {
                        ["items"] = ["All selected items must belong to the same restaurant as the scanned table."]
                    });
            }

            if (!menuItem.IsAvailable || !menuItem.MenuCategory.IsActive)
            {
                throw new TableOrderingServiceException(
                    "Order contains unavailable menu items.",
                    StatusCodes.Status400BadRequest,
                    new Dictionary<string, string[]>
                    {
                        ["items"] = ["Remove unavailable items before submitting the order."]
                    });
            }
        }

        var validOrderItems = request.Items
            .Select(item => (item.MenuItemId, item.Quantity, menuItemsById[item.MenuItemId].Price))
            .ToList();

        var nowUtc = DateTime.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RestaurantId = table.RestaurantId,
            RestaurantTableId = table.RestaurantTableId,
            Status = OrderStatus.OrderReceived,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            OrderItems = validOrderItems
                .Select(item => new OrderItem
                {
                    Id = Guid.NewGuid(),
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                })
                .ToList()
        };

        var totalAmount = order.OrderItems.Sum(item => item.UnitPrice * item.Quantity);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.Orders.Add(order);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SubmittedTableOrderResponseDto
        {
            OrderId = order.Id,
            UserId = order.UserId,
            RestaurantId = order.RestaurantId,
            RestaurantTableId = order.RestaurantTableId,
            TableNumber = table.TableNumber,
            OrderName = $"Table {table.TableNumber}",
            Status = order.Status.ToString(),
            ItemCount = order.OrderItems.Sum(item => item.Quantity),
            TotalAmount = totalAmount,
            CreatedAtUtc = order.CreatedAtUtc
        };
    }
}
