namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class AdminPendingRestaurantDto
{
    public Guid Id { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerPhoneNumber { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
