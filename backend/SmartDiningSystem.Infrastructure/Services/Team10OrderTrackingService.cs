using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.Orders;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class Team10OrderTrackingService : ITeam10OrderTrackingService
{
    private static readonly OrderStatus[] ActiveOrderStatuses =
    [
        OrderStatus.OrderReceived,
        OrderStatus.Preparing,
        OrderStatus.Ready
    ];

    private readonly AppDbContext _dbContext;

    public Team10OrderTrackingService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Team10OrderTrackingListItemDto>> GetActiveOrdersForTrackingAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Where(order => ActiveOrderStatuses.Contains(order.Status))
            .OrderByDescending(order => order.UpdatedAtUtc)
            .ThenByDescending(order => order.CreatedAtUtc)
            .Select(order => new Team10OrderTrackingListItemDto
            {
                OrderId = order.Id,
                RestaurantId = order.RestaurantId,
                RestaurantName = order.Restaurant != null ? order.Restaurant.Name : string.Empty,
                TableId = order.RestaurantTableId,
                TableNumber = order.RestaurantTable != null ? order.RestaurantTable.TableNumber : 0,
                UserId = order.UserId,
                CreatedAt = order.CreatedAtUtc,
                UpdatedAt = order.UpdatedAtUtc,
                ItemsCount = order.OrderItems.Sum(item => item.Quantity),
                TotalPrice = order.OrderItems.Sum(item => item.UnitPrice * item.Quantity),
                Currency = "IQD",
                IsActive = true
            })
            .ToListAsync(cancellationToken);
    }
}
