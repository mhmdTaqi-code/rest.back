namespace SmartDiningSystem.Application.DTOs.Bookings;

public class OwnerTableReleaseResponseDto
{
    public Guid RestaurantId { get; set; }
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public string PreviousOccupancyStatus { get; set; } = string.Empty;
    public string NewOccupancyStatus { get; set; } = string.Empty;
    public DateTime ReleasedAtUtc { get; set; }
    public Guid ReleasedByUserId { get; set; }
    public Guid ClosedSessionId { get; set; }
    public bool CanAcceptNewBooking { get; set; }
    public bool CanAcceptOrdering { get; set; }
}
