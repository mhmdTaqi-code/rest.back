using SmartDiningSystem.Application.DTOs.Orders;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IOwnerOrderWorkflowService
{
    Task<IReadOnlyList<OwnerActiveOrderDto>> GetActiveOrdersAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<OwnerOrderDetailDto> GetOrderDetailsAsync(Guid ownerId, Guid orderId, CancellationToken cancellationToken);
    Task<OwnerOrderDetailDto> UpdateOrderStatusAsync(
        Guid ownerId,
        Guid orderId,
        UpdateOrderStatusRequestDto request,
        CancellationToken cancellationToken);
}
