using SmartDiningSystem.Application.DTOs.TableOrdering;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IPublicTableMenuService
{
    Task<PublicTableMenuResponseDto> GetPublicMenuAsync(
        Guid restaurantId,
        Guid tableId,
        CancellationToken cancellationToken);
}
