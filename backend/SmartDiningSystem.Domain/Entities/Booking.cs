using SmartDiningSystem.Domain.Enums;

namespace SmartDiningSystem.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid RestaurantTableId { get; set; }
    public DateTime ReservationTimeUtc { get; set; }
    public BookingStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CheckedInAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime? NoShowMarkedAtUtc { get; set; }

    public UserAccount? User { get; set; }
    public Restaurant? Restaurant { get; set; }
    public RestaurantTable? RestaurantTable { get; set; }
    public ICollection<BookingItem> Items { get; set; } = new List<BookingItem>();
    public ICollection<TableSession> TableSessions { get; set; } = new List<TableSession>();
}
