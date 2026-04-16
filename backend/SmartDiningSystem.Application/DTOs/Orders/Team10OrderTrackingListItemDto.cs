namespace SmartDiningSystem.Application.DTOs.Orders;

public class Team10OrderTrackingListItemDto
{
    public Guid OrderId { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ItemsCount { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "IQD";
    public bool IsActive { get; set; }
}
