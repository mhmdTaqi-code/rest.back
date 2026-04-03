using SmartDiningSystem.Domain.Enums;

namespace SmartDiningSystem.Domain.Entities;

public class TableSession
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid RestaurantTableId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? UserId { get; set; }
    public TableSessionStatus Status { get; set; }
    public DateTime OpenedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public Guid? ClosedByUserAccountId { get; set; }
    public string? CloseReason { get; set; }

    public Restaurant? Restaurant { get; set; }
    public RestaurantTable? RestaurantTable { get; set; }
    public Booking? Booking { get; set; }
    public UserAccount? User { get; set; }
    public UserAccount? ClosedByUserAccount { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
