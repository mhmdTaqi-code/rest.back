namespace SmartDiningSystem.Application.DTOs.Reservations;

public class CreateReservationRequestDto
{
    public Guid RestaurantId { get; set; }
    public Guid RestaurantTableId { get; set; }
    public DateTime ReservationStartUtc { get; set; }
    public int GuestCount { get; set; }
}
