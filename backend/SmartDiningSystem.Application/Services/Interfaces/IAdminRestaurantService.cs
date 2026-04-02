using SmartDiningSystem.Application.DTOs.Restaurants;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IAdminRestaurantService
{
    Task<IReadOnlyList<AdminPendingRestaurantDto>> GetPendingRestaurantsAsync(CancellationToken cancellationToken);
    Task<AdminRestaurantDetailsDto> CreateRestaurantForOwnerAsync(AdminCreateRestaurantRequestDto request, CancellationToken cancellationToken);

    Task<AdminRestaurantDetailsDto> GetRestaurantDetailsAsync(Guid restaurantId, CancellationToken cancellationToken);

    Task<AdminRestaurantDetailsDto> ApproveRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken);

    Task<AdminRestaurantDetailsDto> RejectRestaurantAsync(
        Guid restaurantId,
        string rejectionReason,
        CancellationToken cancellationToken);
}
