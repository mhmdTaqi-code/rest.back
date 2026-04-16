namespace SmartDiningSystem.Application.DTOs.Orders;

public class UserOrderHistoryDto
{
    public Guid OrderId { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public Guid? TableId { get; set; }
    public int TableNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<UserOrderHistoryItemDto> Items { get; set; } = new();
}
