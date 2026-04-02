namespace SmartDiningSystem.Domain.Entities;

public class RestaurantTable
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public int TableNumber { get; set; }
    public string? ImageUrl { get; set; }
    public string TableToken { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Restaurant? Restaurant { get; set; }
    public ICollection<TableCart> TableCarts { get; set; } = new List<TableCart>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<TableSession> TableSessions { get; set; } = new List<TableSession>();
}
