namespace SmartDiningSystem.Application.DTOs.TableOrdering;

public class SubmittedTableOrderResponseDto
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public Guid RestaurantTableId { get; set; }
    public int TableNumber { get; set; }
    public string TableToken { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
