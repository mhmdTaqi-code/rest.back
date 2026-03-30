namespace SmartDiningSystem.Application.DTOs.RestaurantTables;

public class UpdateRestaurantTableStatusRequestDto
{
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
}
