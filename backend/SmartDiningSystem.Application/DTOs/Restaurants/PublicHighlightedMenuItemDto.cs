namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class PublicHighlightedMenuItemDto
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? CategoryName { get; set; }
    public string HighlightTag { get; set; } = string.Empty;
}
