using SmartDiningSystem.Application.DTOs.MenuManagement;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<MenuCategoryDto>> GetOwnerCategoriesAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<MenuCategoryDto> CreateCategoryAsync(Guid ownerId, CreateCategoryRequestDto request, CancellationToken cancellationToken);
    Task<MenuCategoryDto> UpdateCategoryAsync(Guid ownerId, Guid categoryId, UpdateCategoryRequestDto request, CancellationToken cancellationToken);
    Task DeleteCategoryAsync(Guid ownerId, Guid categoryId, CancellationToken cancellationToken);
}
