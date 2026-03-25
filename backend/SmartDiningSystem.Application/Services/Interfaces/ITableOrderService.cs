using SmartDiningSystem.Application.DTOs.TableOrdering;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface ITableOrderService
{
    Task<SubmittedTableOrderResponseDto> SubmitOrderAsync(
        Guid userId,
        Guid restaurantId,
        Guid tableId,
        SubmitTableOrderRequestDto request,
        CancellationToken cancellationToken);
}
