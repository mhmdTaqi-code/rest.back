namespace SmartDiningSystem.Application.DTOs.Orders;

public class OwnerActiveOrderDto
{
    public Guid OrderId { get; set; }
    public Guid RestaurantId { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatingsCount { get; set; }
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public IReadOnlyList<OwnerOrderItemDto> Items { get; set; } = Array.Empty<OwnerOrderItemDto>();
}
