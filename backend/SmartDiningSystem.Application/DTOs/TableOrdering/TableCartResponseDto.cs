namespace SmartDiningSystem.Application.DTOs.TableOrdering;

public class TableCartResponseDto
{
    public Guid CartId { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public Guid RestaurantTableId { get; set; }
    public int TableNumber { get; set; }
    public string TableDisplayName { get; set; } = string.Empty;
    public string TableToken { get; set; } = string.Empty;
    public IReadOnlyList<TableCartItemDto> Items { get; set; } = Array.Empty<TableCartItemDto>();
    public decimal TotalAmount { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
