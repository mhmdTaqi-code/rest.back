namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class RestaurantRatingSummaryDto
{
    public Guid RestaurantId { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatingsCount { get; set; }
    public IReadOnlyList<RestaurantRatingDistributionItemDto> Distribution { get; set; }
        = Array.Empty<RestaurantRatingDistributionItemDto>();
}
