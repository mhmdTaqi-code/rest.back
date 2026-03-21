namespace SmartDiningSystem.Domain.Entities;

public class TableCart
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid RestaurantTableId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public UserAccount? User { get; set; }
    public Restaurant? Restaurant { get; set; }
    public RestaurantTable? RestaurantTable { get; set; }
    public ICollection<TableCartItem> Items { get; set; } = new List<TableCartItem>();
}
