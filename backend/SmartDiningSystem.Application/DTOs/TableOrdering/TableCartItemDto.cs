namespace SmartDiningSystem.Application.DTOs.TableOrdering;

public class TableCartItemDto
{
    public Guid CartItemId { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid MenuCategoryId { get; set; }
    public string MenuCategoryName { get; set; } = string.Empty;
    public string MenuItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsAvailable { get; set; }
}
