using SmartDiningSystem.Application.DTOs.Orders;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IUserOrderTrackingService
{
    Task<UserOrderStatusDto> GetOrderStatusAsync(Guid userId, Guid orderId, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserOrderHistoryDto>> GetOrderHistoryAsync(Guid userId, Guid? restaurantId, CancellationToken cancellationToken);
}
