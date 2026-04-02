namespace SmartDiningSystem.Application.DTOs.Bookings;

public class OwnerRestaurantTableLiveStatusDto
{
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? CurrentBookingId { get; set; }
    public string? CurrentBookingStatus { get; set; }
    public DateTime? ReservationTimeUtc { get; set; }
}
