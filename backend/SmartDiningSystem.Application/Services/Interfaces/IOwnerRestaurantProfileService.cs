using SmartDiningSystem.Application.DTOs.Restaurants;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IOwnerRestaurantProfileService
{
    Task<OwnerRestaurantStatusDto> UpdateRestaurantImageAsync(
        Guid ownerId,
        UpdateRestaurantImageRequestDto request,
        CancellationToken cancellationToken);

    Task<OwnerRestaurantStatusDto> UpdateRestaurantLocationAsync(
        Guid ownerId,
        UpdateRestaurantLocationRequestDto request,
        CancellationToken cancellationToken);
}
