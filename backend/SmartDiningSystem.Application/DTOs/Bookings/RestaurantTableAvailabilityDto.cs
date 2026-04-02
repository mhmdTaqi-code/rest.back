namespace SmartDiningSystem.Application.DTOs.Bookings;

public class RestaurantTableAvailabilityDto
{
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public bool IsAvailable { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ReservationTimeUtc { get; set; }
}
