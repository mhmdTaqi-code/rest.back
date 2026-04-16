namespace SmartDiningSystem.Application.DTOs.TableAvailability;

public class TableAvailabilityStateDto
{
    public Guid TableId { get; set; }
    public Guid RestaurantId { get; set; }
    public int TableNumber { get; set; }
    public bool IsTableActive { get; set; }
    public bool IsOrderingEnabled { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public bool HasActiveSession { get; set; }
    public Guid? ActiveSessionId { get; set; }
    public Guid? ActiveSessionUserId { get; set; }
    public Guid? ActiveSessionBookingId { get; set; }
    public DateTime? ActiveSessionOpenedAtUtc { get; set; }
    public bool HasActiveBooking { get; set; }
    public Guid? ActiveBookingId { get; set; }
    public Guid? ActiveBookingUserId { get; set; }
    public string? ActiveBookingStatus { get; set; }
    public DateTime? ReservationTimeUtc { get; set; }
}
