namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class UpdateOwnerRestaurantRequestDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? ContactPhone { get; set; }
    public string? ImageUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
