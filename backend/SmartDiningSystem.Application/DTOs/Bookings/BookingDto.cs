namespace SmartDiningSystem.Application.DTOs.Bookings;

public class BookingDto
{
    public Guid BookingId { get; set; }
    public Guid UserId { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid RestaurantTableId { get; set; }
    public int TableNumber { get; set; }
    public DateTime ReservationTimeUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CheckedInAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime? NoShowMarkedAtUtc { get; set; }
    public DateTime? ExpiredAtUtc { get; set; }
    public Guid? SessionId { get; set; }
    public IReadOnlyList<BookingItemDto> Items { get; set; } = Array.Empty<BookingItemDto>();
}
