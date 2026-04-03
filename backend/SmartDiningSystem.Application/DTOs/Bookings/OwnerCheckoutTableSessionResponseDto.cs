namespace SmartDiningSystem.Application.DTOs.Bookings;

public class OwnerCheckoutTableSessionResponseDto
{
    public Guid SessionId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid RestaurantTableId { get; set; }
    public int TableNumber { get; set; }
    public string SessionStatus { get; set; } = string.Empty;
    public string? BookingStatus { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public string? CloseReason { get; set; }
}
