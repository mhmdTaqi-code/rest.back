namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class PublicRestaurantTableDto
{
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
}
