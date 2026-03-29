namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class RestaurantRatingDto
{
    public Guid RatingId { get; set; }
    public Guid RestaurantId { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatingsCount { get; set; }
    public Guid UserId { get; set; }
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
