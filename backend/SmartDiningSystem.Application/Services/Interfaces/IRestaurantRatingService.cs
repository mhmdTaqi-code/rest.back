using SmartDiningSystem.Application.DTOs.Restaurants;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IRestaurantRatingService
{
    Task<RestaurantRatingDto> UpsertRatingAsync(
        Guid userId,
        Guid restaurantId,
        SubmitRestaurantRatingRequestDto request,
        CancellationToken cancellationToken);

    Task<RestaurantRatingDto?> GetUserRatingAsync(
        Guid userId,
        Guid restaurantId,
        CancellationToken cancellationToken);

    Task<RestaurantRatingSummaryDto> GetRatingSummaryAsync(
        Guid restaurantId,
        CancellationToken cancellationToken);
}
