namespace SmartDiningSystem.Application.DTOs.Bookings;

public class PublicRestaurantBookingDto
{
    public Guid BookingId { get; set; }
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public string ReservationTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
