namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class RecommendedRestaurantDto
{
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string Address { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int TotalRatingsCount { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;
}
