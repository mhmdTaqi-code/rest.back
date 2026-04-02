namespace SmartDiningSystem.Application.DTOs.Bookings;

public class CreateBookingItemRequestDto
{
    public Guid MenuItemId { get; set; }
    public int Quantity { get; set; }
}
