namespace SmartDiningSystem.Application.DTOs.Bookings;

public class PublicRestaurantTableLiveStatusDto
{
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsAvailableForNewBooking { get; set; }
}
