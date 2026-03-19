using SmartDiningSystem.Application.DTOs.Restaurants;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IRestaurantQueryService
{
    Task<IReadOnlyList<PublicRestaurantSummaryDto>> GetPublicRestaurantsAsync(CancellationToken cancellationToken);

    Task<OwnerRestaurantStatusDto> GetOwnerRestaurantStatusAsync(Guid ownerId, CancellationToken cancellationToken);
}
