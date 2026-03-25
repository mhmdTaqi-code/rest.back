using SmartDiningSystem.Application.DTOs.TableOrdering;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IRestaurantTableAccessService
{
    Task<ResolvedRestaurantTableDto> ResolveTableAsync(
        Guid restaurantId,
        Guid tableId,
        CancellationToken cancellationToken);
}
