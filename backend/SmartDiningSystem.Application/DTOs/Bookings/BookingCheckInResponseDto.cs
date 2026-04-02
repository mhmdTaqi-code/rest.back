namespace SmartDiningSystem.Application.DTOs.Bookings;

public class BookingCheckInResponseDto
{
    public Guid BookingId { get; set; }
    public Guid SessionId { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid RestaurantTableId { get; set; }
    public int TableNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CheckedInAtUtc { get; set; }
}
