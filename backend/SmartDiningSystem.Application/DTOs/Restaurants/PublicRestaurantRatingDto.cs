namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class PublicRestaurantRatingDto
{
    public Guid RatingId { get; set; }
    public Guid RestaurantId { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatingsCount { get; set; }
    public PublicRatingUserDto User { get; set; } = new();
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
