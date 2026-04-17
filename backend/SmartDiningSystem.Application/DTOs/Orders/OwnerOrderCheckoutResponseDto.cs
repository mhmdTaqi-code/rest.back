namespace SmartDiningSystem.Application.DTOs.Orders;

public class OwnerOrderCheckoutResponseDto
{
    public Guid OrderId { get; set; }
    public Guid TableId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public bool TableReleased { get; set; }
}
