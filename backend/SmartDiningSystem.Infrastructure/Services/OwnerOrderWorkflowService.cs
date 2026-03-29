using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.Orders;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class OwnerOrderWorkflowService : IOwnerOrderWorkflowService
{
    private readonly AppDbContext _dbContext;

    public OwnerOrderWorkflowService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OwnerActiveOrderDto>> GetActiveOrdersAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, cancellationToken);

        var orders = await LoadOwnedOrdersQuery(restaurant.Id)
            .Where(order => order.Status != OrderStatus.Served)
            .OrderBy(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var averageRating = CalculateAverageRating(restaurant);
        var totalRatingsCount = restaurant.Ratings.Count;

        return orders
            .Select(order => MapActiveOrder(order, averageRating, totalRatingsCount))
            .ToList();
    }

    public async Task<OwnerOrderDetailDto> GetOrderDetailsAsync(Guid ownerId, Guid orderId, CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, cancellationToken);
        var order = await GetOwnedOrderAsync(restaurant.Id, orderId, cancellationToken);
        return MapOrderDetail(order, CalculateAverageRating(restaurant), restaurant.Ratings.Count);
    }

    public async Task<OwnerOrderDetailDto> UpdateOrderStatusAsync(
        Guid ownerId,
        Guid orderId,
        UpdateOrderStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, cancellationToken);
        var order = await GetOwnedOrderAsync(restaurant.Id, orderId, cancellationToken, asNoTracking: false);

        EnsureOrderHasItems(order);

        var nextStatus = ParseRequestedStatus(request.Status);
        EnsureTransitionAllowed(order.Status, nextStatus);

        order.Status = nextStatus;
        order.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapOrderDetail(order, CalculateAverageRating(restaurant), restaurant.Ratings.Count);
    }

    private IQueryable<Order> LoadOwnedOrdersQuery(Guid restaurantId)
    {
        return _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.RestaurantId == restaurantId)
            .Include(order => order.RestaurantTable)
            .Include(order => order.User)
            .Include(order => order.OrderItems)
            .ThenInclude(item => item.MenuItem);
    }

    private async Task<Restaurant> GetApprovedOwnerRestaurantAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var restaurant = await _dbContext.Restaurants
            .Include(entity => entity.Ratings)
            .OrderBy(entity => entity.CreatedAtUtc)
            .FirstOrDefaultAsync(entity => entity.OwnerId == ownerId, cancellationToken);

        if (restaurant is null)
        {
            throw new OwnerOrderWorkflowServiceException(
                "Restaurant was not found for this owner.",
                StatusCodes.Status404NotFound);
        }

        if (restaurant.ApprovalStatus != RestaurantApprovalStatus.Approved)
        {
            throw new OwnerOrderWorkflowServiceException(
                "Only approved restaurant owners can access restaurant orders.",
                StatusCodes.Status403Forbidden,
                new Dictionary<string, string[]>
                {
                    ["approvalStatus"] = [restaurant.ApprovalStatus.ToString()]
                });
        }

        return restaurant;
    }

    private async Task<Order> GetOwnedOrderAsync(
        Guid restaurantId,
        Guid orderId,
        CancellationToken cancellationToken,
        bool asNoTracking = true)
    {
        IQueryable<Order> query = _dbContext.Orders;
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var order = await query
            .Include(entity => entity.RestaurantTable)
            .Include(entity => entity.User)
            .Include(entity => entity.OrderItems)
            .ThenInclude(item => item.MenuItem)
            .FirstOrDefaultAsync(
                entity => entity.Id == orderId && entity.RestaurantId == restaurantId,
                cancellationToken);

        if (order is null)
        {
            throw new OwnerOrderWorkflowServiceException(
                "Order was not found for this restaurant.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["orderId"] = ["The selected order was not found for this restaurant."]
                });
        }

        return order;
    }

    private static void EnsureOrderHasItems(Order order)
    {
        if (order.OrderItems.Count == 0)
        {
            throw new OwnerOrderWorkflowServiceException(
                "Order does not contain any items.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["orderId"] = ["The selected order does not contain any items."]
                });
        }
    }

    private static OrderStatus ParseRequestedStatus(string status)
    {
        if (string.Equals(status, "OrderReceived", StringComparison.OrdinalIgnoreCase))
        {
            return OrderStatus.OrderReceived;
        }

        if (Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
        {
            return parsedStatus == OrderStatus.Received
                ? OrderStatus.OrderReceived
                : parsedStatus;
        }

        throw new OwnerOrderWorkflowServiceException(
            "Requested order status is invalid.",
            StatusCodes.Status400BadRequest,
            new Dictionary<string, string[]>
            {
                ["status"] = ["Status must be one of: OrderReceived, Preparing, Ready, Served."]
            });
    }

    private static void EnsureTransitionAllowed(OrderStatus currentStatus, OrderStatus nextStatus)
    {
        currentStatus = NormalizeStatus(currentStatus);
        nextStatus = NormalizeStatus(nextStatus);

        if (currentStatus == OrderStatus.Served)
        {
            throw new OwnerOrderWorkflowServiceException(
                "Served orders cannot be updated.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["status"] = ["Served orders cannot be updated."]
                });
        }

        var allowedNextStatus = currentStatus switch
        {
            OrderStatus.OrderReceived => OrderStatus.Preparing,
            OrderStatus.Preparing => OrderStatus.Ready,
            OrderStatus.Ready => OrderStatus.Served,
            _ => throw new OwnerOrderWorkflowServiceException(
                "Current order status is invalid.",
                StatusCodes.Status400BadRequest)
        };

        if (nextStatus != allowedNextStatus)
        {
            throw new OwnerOrderWorkflowServiceException(
                "Order status transition is invalid.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["status"] = [$"Allowed next status is {ToApiStatus(allowedNextStatus)}."]
                });
        }
    }

    private static OrderStatus NormalizeStatus(OrderStatus status)
    {
        return status == OrderStatus.Received ? OrderStatus.OrderReceived : status;
    }

    private static OwnerActiveOrderDto MapActiveOrder(Order order, double averageRating, int totalRatingsCount)
    {
        return new OwnerActiveOrderDto
        {
            OrderId = order.Id,
            RestaurantId = order.RestaurantId,
            AverageRating = averageRating,
            TotalRatingsCount = totalRatingsCount,
            TableId = order.RestaurantTableId,
            TableNumber = order.RestaurantTable?.TableNumber ?? 0,
            UserId = order.UserId,
            UserFullName = order.User?.FullName ?? string.Empty,
            Status = ToApiStatus(order.Status),
            TotalPrice = order.OrderItems.Sum(item => item.UnitPrice * item.Quantity),
            CreatedAtUtc = order.CreatedAtUtc,
            Items = order.OrderItems.Select(MapItem).ToList()
        };
    }

    private static OwnerOrderDetailDto MapOrderDetail(Order order, double averageRating, int totalRatingsCount)
    {
        return new OwnerOrderDetailDto
        {
            OrderId = order.Id,
            RestaurantId = order.RestaurantId,
            AverageRating = averageRating,
            TotalRatingsCount = totalRatingsCount,
            TableId = order.RestaurantTableId,
            TableNumber = order.RestaurantTable?.TableNumber ?? 0,
            UserId = order.UserId,
            UserFullName = order.User?.FullName ?? string.Empty,
            UserPhoneNumber = order.User?.PhoneNumber ?? string.Empty,
            Status = ToApiStatus(order.Status),
            TotalPrice = order.OrderItems.Sum(item => item.UnitPrice * item.Quantity),
            CreatedAtUtc = order.CreatedAtUtc,
            UpdatedAtUtc = order.UpdatedAtUtc,
            Items = order.OrderItems.Select(MapItem).ToList()
        };
    }

    private static double CalculateAverageRating(Restaurant restaurant)
    {
        return Math.Round(restaurant.Ratings.Select(rating => (double)rating.Stars).DefaultIfEmpty().Average(), 2);
    }

    private static OwnerOrderItemDto MapItem(OrderItem item)
    {
        return new OwnerOrderItemDto
        {
            OrderItemId = item.Id,
            MenuItemId = item.MenuItemId,
            MenuItemName = item.MenuItem?.Name ?? string.Empty,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            LineTotal = item.UnitPrice * item.Quantity
        };
    }

    private static string ToApiStatus(OrderStatus status)
    {
        status = NormalizeStatus(status);

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
