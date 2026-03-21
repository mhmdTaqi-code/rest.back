using SmartDiningSystem.Application.DTOs.TableOrdering;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface ITableCartService
{
    Task<TableCartResponseDto> GetCurrentCartAsync(Guid userId, string tableToken, CancellationToken cancellationToken);
    Task<TableCartResponseDto> AddItemAsync(Guid userId, string tableToken, AddCartItemRequestDto request, CancellationToken cancellationToken);
    Task<TableCartResponseDto> UpdateItemAsync(Guid userId, string tableToken, Guid cartItemId, UpdateCartItemRequestDto request, CancellationToken cancellationToken);
    Task<TableCartResponseDto> RemoveItemAsync(Guid userId, string tableToken, Guid cartItemId, CancellationToken cancellationToken);
}
