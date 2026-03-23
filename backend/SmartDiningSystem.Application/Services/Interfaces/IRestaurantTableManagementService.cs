using SmartDiningSystem.Application.DTOs.RestaurantTables;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IRestaurantTableManagementService
{
    Task<IReadOnlyList<RestaurantTableDto>> GetOwnerTablesAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<RestaurantTableDto> CreateTableAsync(Guid ownerId, CreateRestaurantTableRequestDto request, CancellationToken cancellationToken);
    Task<IReadOnlyList<RestaurantTableDto>> BulkCreateTablesAsync(Guid ownerId, BulkCreateRestaurantTablesRequestDto request, CancellationToken cancellationToken);
    Task<RestaurantTableDto> UpdateTableStatusAsync(Guid ownerId, Guid tableId, UpdateRestaurantTableStatusRequestDto request, CancellationToken cancellationToken);
    Task DeleteTableAsync(Guid ownerId, Guid tableId, CancellationToken cancellationToken);
}
