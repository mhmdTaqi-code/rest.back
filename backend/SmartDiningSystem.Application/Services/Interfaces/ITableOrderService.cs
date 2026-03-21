using SmartDiningSystem.Application.DTOs.TableOrdering;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface ITableOrderService
{
    Task<SubmittedTableOrderResponseDto> SubmitOrderAsync(
        Guid userId,
        string tableToken,
        SubmitTableOrderRequestDto request,
        CancellationToken cancellationToken);
}
