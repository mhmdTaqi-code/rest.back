using SmartDiningSystem.Application.DTOs.MenuManagement;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IMenuItemService
{
    Task<IReadOnlyList<MenuItemDto>> GetOwnerMenuItemsAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<MenuItemDto> CreateMenuItemAsync(Guid ownerId, CreateMenuItemRequestDto request, CancellationToken cancellationToken);
    Task<MenuItemDto> UpdateMenuItemAsync(Guid ownerId, Guid menuItemId, UpdateMenuItemRequestDto request, CancellationToken cancellationToken);
    Task<MenuItemDto> ToggleAvailabilityAsync(Guid ownerId, Guid menuItemId, ToggleMenuItemAvailabilityRequestDto request, CancellationToken cancellationToken);
    Task DeleteMenuItemAsync(Guid ownerId, Guid menuItemId, CancellationToken cancellationToken);
}
