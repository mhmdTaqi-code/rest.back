namespace SmartDiningSystem.Application.DTOs.TableOrdering;

public class PublicTableMenuCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public IReadOnlyList<PublicTableMenuItemDto> Items { get; set; } = Array.Empty<PublicTableMenuItemDto>();
}
