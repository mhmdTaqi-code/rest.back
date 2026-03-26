using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.Orders;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class UserOrderTrackingService : IUserOrderTrackingService
{
    private readonly AppDbContext _dbContext;

    public UserOrderTrackingService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserOrderStatusDto> GetOrderStatusAsync(Guid userId, Guid orderId, CancellationToken cancellationToken)
    {
        if (orderId == Guid.Empty)
        {
            throw new UserOrderTrackingServiceException(
                "Order id is required.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["orderId"] = ["Order id is required."]
                });
        }

        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(entity => entity.Restaurant)
            .Include(entity => entity.RestaurantTable)
            .FirstOrDefaultAsync(entity => entity.Id == orderId && entity.UserId == userId, cancellationToken);

        if (order is null)
        {
            throw new UserOrderTrackingServiceException(
                "Order was not found for this user.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["orderId"] = ["The selected order was not found for this user."]
                });
        }

        return new UserOrderStatusDto
        {
            OrderId = order.Id,
            RestaurantId = order.RestaurantId,
            RestaurantName = order.Restaurant?.Name ?? string.Empty,
            TableId = order.RestaurantTableId,
            TableNumber = order.RestaurantTable?.TableNumber ?? 0,
            Status = ToApiStatus(order.Status),
            CreatedAtUtc = order.CreatedAtUtc,
            UpdatedAtUtc = order.UpdatedAtUtc
        };
    }

    private static string ToApiStatus(OrderStatus status)
    {
        if (status == OrderStatus.Received)
        {
            status = OrderStatus.OrderReceived;
        }

        return status switch
        {
            OrderStatus.OrderReceived => "OrderReceived",
            OrderStatus.Preparing => "Preparing",
            OrderStatus.Ready => "Ready",
            OrderStatus.Served => "Served",
            _ => status.ToString()
        };
    }
}
