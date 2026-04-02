using SmartDiningSystem.Application.DTOs.RestaurantTables;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IRestaurantTableManagementService
{
    Task<IReadOnlyList<RestaurantTableDto>> GetOwnerTablesAsync(Guid ownerId, Guid restaurantId, CancellationToken cancellationToken);
    Task<RestaurantTableDto> CreateTableAsync(Guid ownerId, Guid restaurantId, CreateRestaurantTableRequestDto request, CancellationToken cancellationToken);
    Task<IReadOnlyList<RestaurantTableDto>> BulkCreateTablesAsync(Guid ownerId, Guid restaurantId, BulkCreateRestaurantTablesRequestDto request, CancellationToken cancellationToken);
    Task<RestaurantTableDto> UpdateTableStatusAsync(Guid ownerId, Guid restaurantId, Guid tableId, UpdateRestaurantTableStatusRequestDto request, CancellationToken cancellationToken);
    Task DeleteTableAsync(Guid ownerId, Guid restaurantId, Guid tableId, CancellationToken cancellationToken);
}
