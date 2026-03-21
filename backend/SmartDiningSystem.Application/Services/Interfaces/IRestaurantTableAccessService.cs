using SmartDiningSystem.Application.DTOs.TableOrdering;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IRestaurantTableAccessService
{
    Task<ResolvedRestaurantTableDto> ResolveTableAsync(string tableToken, CancellationToken cancellationToken);
}
