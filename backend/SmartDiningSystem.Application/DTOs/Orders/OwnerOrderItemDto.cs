namespace SmartDiningSystem.Application.DTOs.Orders;

public class OwnerOrderItemDto
{
    public Guid OrderItemId { get; set; }
    public Guid MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
