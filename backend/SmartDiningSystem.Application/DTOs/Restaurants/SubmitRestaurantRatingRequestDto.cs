namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class SubmitRestaurantRatingRequestDto
{
    public int Stars { get; set; }
    public string? Comment { get; set; }
}
