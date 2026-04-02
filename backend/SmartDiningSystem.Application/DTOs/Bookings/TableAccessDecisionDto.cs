namespace SmartDiningSystem.Application.DTOs.Bookings;

public class TableAccessDecisionDto
{
    public string AccessMode { get; set; } = string.Empty;
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? SessionId { get; set; }
    public bool IsCheckedIn { get; set; }
    public string Message { get; set; } = string.Empty;
}
