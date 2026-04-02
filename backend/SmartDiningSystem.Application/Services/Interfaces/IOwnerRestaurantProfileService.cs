using SmartDiningSystem.Application.DTOs.Restaurants;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IOwnerRestaurantProfileService
{
    Task<IReadOnlyList<OwnerRestaurantStatusDto>> GetRestaurantsAsync(
        Guid ownerId,
        CancellationToken cancellationToken);

    Task<OwnerRestaurantStatusDto> CreateRestaurantAsync(
        Guid ownerId,
        CreateOwnerRestaurantRequestDto request,
        CancellationToken cancellationToken);

    Task<OwnerRestaurantStatusDto> GetRestaurantAsync(
        Guid ownerId,
        Guid restaurantId,
        CancellationToken cancellationToken);

    Task<OwnerRestaurantStatusDto> UpdateRestaurantAsync(
        Guid ownerId,
        Guid restaurantId,
        UpdateOwnerRestaurantRequestDto request,
        CancellationToken cancellationToken);
}
