namespace SmartDiningSystem.Application.DTOs.Reservations;

public class ReservationDto
{
    public Guid ReservationId { get; set; }
    public Guid UserId { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int TotalRatingsCount { get; set; }
    public Guid RestaurantTableId { get; set; }
    public int TableNumber { get; set; }
    public DateTime ReservationStartUtc { get; set; }
    public DateTime ReservationEndUtc { get; set; }
    public DateTime? GracePeriodEndsAtUtc { get; set; }
    public int GuestCount { get; set; }
    public decimal DepositAmount { get; set; }
    public bool IsDepositPaid { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? DepositPaidAtUtc { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? CheckedInAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? NoShowMarkedAtUtc { get; set; }
    public DateTime? DepositForfeitedAtUtc { get; set; }
}
