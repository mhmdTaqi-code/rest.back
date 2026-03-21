namespace SmartDiningSystem.Application.DTOs.TableOrdering;

public class AddCartItemRequestDto
{
    public Guid MenuItemId { get; set; }
    public int Quantity { get; set; }
}
