using SmartDiningSystem.Application.DTOs.TableOrdering;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface ITableCartService
{
    Task<TableCartResponseDto> GetCurrentCartAsync(Guid userId, Guid restaurantId, Guid tableId, CancellationToken cancellationToken);
    Task<TableCartResponseDto> AddItemAsync(Guid userId, Guid restaurantId, Guid tableId, AddCartItemRequestDto request, CancellationToken cancellationToken);
    Task<TableCartResponseDto> UpdateItemAsync(Guid userId, Guid restaurantId, Guid tableId, Guid cartItemId, UpdateCartItemRequestDto request, CancellationToken cancellationToken);
    Task<TableCartResponseDto> RemoveItemAsync(Guid userId, Guid restaurantId, Guid tableId, Guid cartItemId, CancellationToken cancellationToken);
}
