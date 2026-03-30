namespace SmartDiningSystem.Application.DTOs.RestaurantTables;

public class CreateRestaurantTableRequestDto
{
    public int TableNumber { get; set; }
    public string? ImageUrl { get; set; }
}
