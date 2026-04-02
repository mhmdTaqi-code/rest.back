using SmartDiningSystem.Application.DTOs.MenuManagement;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IMenuItemService
{
    Task<IReadOnlyList<MenuItemDto>> GetOwnerMenuItemsAsync(Guid ownerId, Guid restaurantId, CancellationToken cancellationToken);
    Task<MenuItemDto> CreateMenuItemAsync(Guid ownerId, Guid restaurantId, CreateMenuItemRequestDto request, CancellationToken cancellationToken);
    Task<MenuItemDto> UpdateMenuItemAsync(Guid ownerId, Guid restaurantId, Guid menuItemId, UpdateMenuItemRequestDto request, CancellationToken cancellationToken);
    Task<MenuItemDto> SetMenuItemHighlightAsync(Guid ownerId, Guid restaurantId, Guid menuItemId, SetMenuItemHighlightRequestDto request, CancellationToken cancellationToken);
    Task<MenuItemDto> RemoveMenuItemHighlightAsync(Guid ownerId, Guid restaurantId, Guid menuItemId, CancellationToken cancellationToken);
    Task<MenuItemDto> ToggleAvailabilityAsync(Guid ownerId, Guid restaurantId, Guid menuItemId, ToggleMenuItemAvailabilityRequestDto request, CancellationToken cancellationToken);
    Task DeleteMenuItemAsync(Guid ownerId, Guid restaurantId, Guid menuItemId, CancellationToken cancellationToken);
}
