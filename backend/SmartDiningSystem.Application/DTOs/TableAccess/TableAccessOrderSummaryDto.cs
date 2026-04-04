namespace SmartDiningSystem.Application.DTOs.TableAccess;

public class TableAccessOrderSummaryDto
{
    public Guid OrderId { get; set; }
    public int TableNumber { get; set; }
    public string OrderName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
