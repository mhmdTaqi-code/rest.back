namespace SmartDiningSystem.Application.DTOs.MenuManagement;

public class RecommendedMenuItemDto
{
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public Guid MenuCategoryId { get; set; }
    public string MenuCategoryName { get; set; } = string.Empty;
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;
}
