using SmartDiningSystem.Application.DTOs.TableAccess;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface ITableAccessFlowService
{
    Task<TableAccessScanResponseDto> ProcessScanAsync(Guid? userId, TableAccessScanRequestDto request, CancellationToken cancellationToken);
}
