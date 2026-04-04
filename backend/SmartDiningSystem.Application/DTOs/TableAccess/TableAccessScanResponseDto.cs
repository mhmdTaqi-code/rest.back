namespace SmartDiningSystem.Application.DTOs.TableAccess;

public class TableAccessScanResponseDto
{
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public Guid? BookingId { get; set; }
    public bool HasBooking { get; set; }
    public bool IsBookingOwner { get; set; }
    public bool IsCheckedIn { get; set; }
    public bool CheckInPerformed { get; set; }
    public bool RequiresLogin { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public bool CanOrder { get; set; }
    public bool OrderCreated { get; set; }
    public TableAccessOrderSummaryDto? Order { get; set; }
    public string Message { get; set; } = string.Empty;
}
