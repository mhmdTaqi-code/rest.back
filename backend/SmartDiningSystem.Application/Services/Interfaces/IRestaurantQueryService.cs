using SmartDiningSystem.Application.DTOs.Restaurants;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IRestaurantQueryService
{
    Task<IReadOnlyList<PublicRestaurantSummaryDto>> GetPublicRestaurantsAsync(CancellationToken cancellationToken);
    Task<RestaurantDetailsDto> GetRestaurantByIdAsync(Guid restaurantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicRestaurantTableDto>> GetTablesByRestaurantIdAsync(Guid restaurantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicRestaurantMenuCategoryDto>> GetCategoriesByRestaurantIdAsync(Guid restaurantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicHighlightedMenuItemDto>> GetHighlightedItemsByRestaurantIdAsync(Guid restaurantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicRestaurantMenuItemDto>> GetMenuByRestaurantIdAsync(
        Guid restaurantId,
        GetRestaurantMenuQueryDto query,
        CancellationToken cancellationToken);

}
