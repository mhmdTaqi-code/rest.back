using SmartDiningSystem.Application.DTOs.Orders;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IUserOrderTrackingService
{
    Task<UserOrderStatusDto> GetOrderStatusAsync(Guid userId, Guid orderId, CancellationToken cancellationToken);
}
