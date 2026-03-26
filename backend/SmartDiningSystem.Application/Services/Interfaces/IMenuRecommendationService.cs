using SmartDiningSystem.Application.DTOs.MenuManagement;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IMenuRecommendationService
{
    Task<IReadOnlyList<RecommendedMenuItemDto>> GetRecommendationsAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
