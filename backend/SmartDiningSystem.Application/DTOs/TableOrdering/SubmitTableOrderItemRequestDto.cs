namespace SmartDiningSystem.Application.DTOs.TableOrdering;

public class SubmitTableOrderItemRequestDto
{
    public Guid MenuItemId { get; set; }
    public int Quantity { get; set; }
}
