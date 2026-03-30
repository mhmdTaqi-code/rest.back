namespace SmartDiningSystem.Application.DTOs.MenuManagement;

public class MenuItemDto
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatingsCount { get; set; }
    public Guid MenuCategoryId { get; set; }
    public string MenuCategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsHighlighted { get; set; }
    public string? HighlightTag { get; set; }
    public bool IsAvailable { get; set; }
    public int DisplayOrder { get; set; }
}
