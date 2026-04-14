namespace SmartDiningSystem.Application.DTOs.Orders;

public class UserOrderItemDto
{
    public Guid MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
