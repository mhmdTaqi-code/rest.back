namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class PublicRestaurantMenuCategoryDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
}
