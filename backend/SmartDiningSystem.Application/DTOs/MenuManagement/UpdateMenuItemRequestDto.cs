namespace SmartDiningSystem.Application.DTOs.MenuManagement;

public class UpdateMenuItemRequestDto
{
    public Guid MenuCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int? DisplayOrder { get; set; }
}
