namespace SmartDiningSystem.Domain.Enums;

public enum ReservationStatus
{
    PendingPayment = 1,
    Confirmed = 2,
    CheckedIn = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6
}
