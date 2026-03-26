using SmartDiningSystem.Application.DTOs.Restaurants;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IRestaurantRecommendationService
{
    Task<IReadOnlyList<RecommendedRestaurantDto>> GetRecommendationsAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
