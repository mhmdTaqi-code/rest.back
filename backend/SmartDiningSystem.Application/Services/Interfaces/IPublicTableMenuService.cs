using SmartDiningSystem.Application.DTOs.TableOrdering;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IPublicTableMenuService
{
    Task<PublicTableMenuResponseDto> GetPublicMenuAsync(string tableToken, CancellationToken cancellationToken);
}
