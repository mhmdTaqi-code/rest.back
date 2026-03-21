namespace SmartDiningSystem.Domain.Entities;

public class TableCartItem
{
    public Guid Id { get; set; }
    public Guid TableCartId { get; set; }
    public Guid MenuItemId { get; set; }
    public int Quantity { get; set; }

    public TableCart? TableCart { get; set; }
    public MenuItem? MenuItem { get; set; }
}
