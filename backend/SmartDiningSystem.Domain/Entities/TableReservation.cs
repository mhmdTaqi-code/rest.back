using SmartDiningSystem.Domain.Enums;

namespace SmartDiningSystem.Domain.Entities;

public class TableReservation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid RestaurantTableId { get; set; }
    public DateTime ReservationStartUtc { get; set; }
    public DateTime ReservationEndUtc { get; set; }
    public int GuestCount { get; set; }
    public decimal DepositAmount { get; set; }
    public bool IsDepositPaid { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? DepositPaidAtUtc { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? CheckedInAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? GracePeriodEndsAtUtc { get; set; }
    public DateTime? NoShowMarkedAtUtc { get; set; }
    public DateTime? DepositForfeitedAtUtc { get; set; }

    public UserAccount? User { get; set; }
    public Restaurant? Restaurant { get; set; }
    public RestaurantTable? RestaurantTable { get; set; }
}
