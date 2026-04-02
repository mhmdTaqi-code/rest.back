using SmartDiningSystem.Application.DTOs.TableOrdering;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface ITableSessionOrderService
{
    Task<SubmittedTableOrderResponseDto> SubmitOrderAsync(Guid userId, Guid sessionId, SubmitTableOrderRequestDto request, CancellationToken cancellationToken);
}
