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
        string tableToken,
        SubmitTableOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        _ = request;

        var table = await _restaurantTableAccessService.ResolveTableAsync(tableToken, cancellationToken);

        var userExists = await _dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId && user.IsActive, cancellationToken);

        if (!userExists)
        {
            throw new TableOrderingServiceException(
                "Authenticated user account was not found.",
                StatusCodes.Status401Unauthorized);
        }

        var cart = await _dbContext.TableCarts
            .Include(entity => entity.Items)
            // EF uses this include path to load optional navigations; runtime validation below still guards nulls.
            .ThenInclude(item => item.MenuItem!)
            .ThenInclude(menuItem => menuItem.MenuCategory)
            .FirstOrDefaultAsync(
                entity => entity.UserId == userId && entity.RestaurantTableId == table.RestaurantTableId,
                cancellationToken);

        if (cart is null || cart.Items.Count == 0)
        {
            throw new TableOrderingServiceException(
                "Cart is empty.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["cart"] = ["Add at least one item before submitting an order."]
                });
        }

        foreach (var cartItem in cart.Items)
        {
            var menuItem = cartItem.MenuItem;
            if (menuItem is null)
            {
                throw new TableOrderingServiceException(
                    "Cart contains an invalid menu item.",
                    StatusCodes.Status400BadRequest);
            }

            if (menuItem.RestaurantId != table.RestaurantId ||
                menuItem.MenuCategory is null ||
                menuItem.MenuCategory.RestaurantId != table.RestaurantId)
            {
                throw new TableOrderingServiceException(
                    "Cart contains menu items from a different restaurant.",
                    StatusCodes.Status400BadRequest,
                    new Dictionary<string, string[]>
                    {
                        ["cart"] = ["Cart items must belong to the same restaurant as the scanned table."]
                    });
            }

            if (!menuItem.IsAvailable || !menuItem.MenuCategory.IsActive)
            {
                throw new TableOrderingServiceException(
                    "Cart contains unavailable menu items.",
                    StatusCodes.Status400BadRequest,
                    new Dictionary<string, string[]>
                    {
                        ["cart"] = ["Remove unavailable items before submitting the order."]
                    });
            }
        }

        var validCartItems = new List<(Guid MenuItemId, int Quantity, decimal UnitPrice)>(cart.Items.Count);
        foreach (var cartItem in cart.Items)
        {
            var menuItem = cartItem.MenuItem;
            if (menuItem is null)
            {
                continue;
            }

            validCartItems.Add((cartItem.MenuItemId, cartItem.Quantity, menuItem.Price));
        }

        var nowUtc = DateTime.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RestaurantId = table.RestaurantId,
            RestaurantTableId = table.RestaurantTableId,
            Status = OrderStatus.Received,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            OrderItems = validCartItems
                .Select(item => new OrderItem
                {
                    Id = Guid.NewGuid(),
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                })
                .ToList()
        };

        var totalAmount = order.OrderItems.Sum(item => item.UnitPrice * item.Quantity);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.Orders.Add(order);
        _dbContext.TableCartItems.RemoveRange(cart.Items);
        _dbContext.TableCarts.Remove(cart);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SubmittedTableOrderResponseDto
        {
            OrderId = order.Id,
            UserId = order.UserId,
            RestaurantId = order.RestaurantId,
            RestaurantName = table.RestaurantName,
            RestaurantTableId = order.RestaurantTableId,
            TableNumber = table.TableNumber,
            TableToken = table.TableToken,
            Status = order.Status.ToString(),
            ItemCount = order.OrderItems.Sum(item => item.Quantity),
            TotalAmount = totalAmount,
            CreatedAtUtc = order.CreatedAtUtc
        };
    }
}
