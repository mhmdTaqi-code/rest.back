namespace SmartDiningSystem.Application.DTOs.Bookings;

public class OwnerRestaurantBookingDto
{
    public Guid BookingId { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserPhoneNumber { get; set; } = string.Empty;
    public Guid RestaurantTableId { get; set; }
    public int TableNumber { get; set; }
    public DateTime ReservationTimeUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}
