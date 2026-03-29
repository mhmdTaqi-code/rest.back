namespace SmartDiningSystem.Domain.Entities;

public class RestaurantTable
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public int TableNumber { get; set; }
    public string TableToken { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Restaurant? Restaurant { get; set; }
    public ICollection<TableCart> TableCarts { get; set; } = new List<TableCart>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<TableReservation> Reservations { get; set; } = new List<TableReservation>();
}
