namespace SmartDiningSystem.Application.DTOs.Orders;

public class UserOrderStatusDto
{
    public Guid OrderId { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int TotalRatingsCount { get; set; }
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
