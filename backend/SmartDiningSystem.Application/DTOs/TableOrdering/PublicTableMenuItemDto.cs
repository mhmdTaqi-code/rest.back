namespace SmartDiningSystem.Application.DTOs.TableOrdering;

public class PublicTableMenuItemDto
{
    public Guid Id { get; set; }
    public Guid MenuCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsAvailable { get; set; }
}
