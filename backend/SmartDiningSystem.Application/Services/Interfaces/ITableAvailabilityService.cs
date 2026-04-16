using SmartDiningSystem.Application.DTOs.TableAvailability;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface ITableAvailabilityService
{
    Task<TableAvailabilityStateDto> GetTableStateAsync(Guid tableId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TableAvailabilityStateDto>> GetRestaurantTableStatesAsync(Guid restaurantId, CancellationToken cancellationToken);
    Task<int> ExpireOverdueBookingsAsync(CancellationToken cancellationToken);
}
