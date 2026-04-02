using SmartDiningSystem.Application.DTOs.Orders;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IOwnerOrderWorkflowService
{
    Task<IReadOnlyList<OwnerActiveOrderDto>> GetActiveOrdersAsync(Guid ownerId, Guid restaurantId, CancellationToken cancellationToken);
    Task<OwnerOrderDetailDto> GetOrderDetailsAsync(Guid ownerId, Guid restaurantId, Guid orderId, CancellationToken cancellationToken);
    Task<OwnerOrderDetailDto> UpdateOrderStatusAsync(
        Guid ownerId,
        Guid restaurantId,
        Guid orderId,
        UpdateOrderStatusRequestDto request,
        CancellationToken cancellationToken);
}
