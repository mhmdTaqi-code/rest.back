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
            .Where(entity => entity.Id == orderId && entity.UserId == userId)
            .Select(entity => new UserOrderStatusDto
            {
                OrderId = entity.Id,
                RestaurantId = entity.RestaurantId,
                RestaurantName = entity.Restaurant != null ? entity.Restaurant.Name : string.Empty,
                AverageRating = entity.Restaurant != null
                    ? Math.Round(entity.Restaurant.Ratings.Select(rating => (double?)rating.Stars).Average() ?? 0d, 2)
                    : 0d,
                TotalRatingsCount = entity.Restaurant != null
                    ? entity.Restaurant.Ratings.Count()
                    : 0,
                TableId = entity.RestaurantTableId,
                TableNumber = entity.RestaurantTable != null ? entity.RestaurantTable.TableNumber : 0,
                Status = ToApiStatus(entity.Status),
                CreatedAtUtc = entity.CreatedAtUtc,
                UpdatedAtUtc = entity.UpdatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

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

        return order;
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
