namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class AdminPendingRestaurantDto
{
    public Guid Id { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerPhoneNumber { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string? ImageUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? RestaurantDescription { get; set; }
    public string? RestaurantAddress { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatingsCount { get; set; }
}
