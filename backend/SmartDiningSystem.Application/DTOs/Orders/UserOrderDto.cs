namespace SmartDiningSystem.Application.DTOs.Orders;

public class UserOrderDto
{
    public Guid OrderId { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string TableNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<UserOrderItemDto> Items { get; set; } = Array.Empty<UserOrderItemDto>();
}
