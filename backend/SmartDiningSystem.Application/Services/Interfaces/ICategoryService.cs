using SmartDiningSystem.Application.DTOs.MenuManagement;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<MenuCategoryDto>> GetOwnerCategoriesAsync(Guid ownerId, Guid restaurantId, CancellationToken cancellationToken);
    Task<MenuCategoryDto> CreateCategoryAsync(Guid ownerId, Guid restaurantId, CreateCategoryRequestDto request, CancellationToken cancellationToken);
    Task<MenuCategoryDto> UpdateCategoryAsync(Guid ownerId, Guid restaurantId, Guid categoryId, UpdateCategoryRequestDto request, CancellationToken cancellationToken);
    Task DeleteCategoryAsync(Guid ownerId, Guid restaurantId, Guid categoryId, CancellationToken cancellationToken);
}
