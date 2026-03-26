namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class RestaurantRatingDto
{
    public Guid RestaurantId { get; set; }
    public Guid UserId { get; set; }
    public int Stars { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
