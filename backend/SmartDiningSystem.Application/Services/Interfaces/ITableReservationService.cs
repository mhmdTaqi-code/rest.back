using SmartDiningSystem.Application.DTOs.Reservations;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface ITableReservationService
{
    Task<ReservationDto> CreateReservationAsync(Guid userId, CreateReservationRequestDto request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReservationDto>> GetMyReservationsAsync(Guid userId, CancellationToken cancellationToken);
    Task<ReservationDto> GetMyReservationAsync(Guid userId, Guid reservationId, CancellationToken cancellationToken);
    Task<ReservationDto> CancelReservationAsync(Guid userId, Guid reservationId, CancelReservationRequestDto? request, CancellationToken cancellationToken);
    Task<ReservationDto> MarkDepositPaidAsync(Guid userId, Guid reservationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OwnerReservationDto>> GetOwnerReservationsAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<OwnerReservationDto> GetOwnerReservationAsync(Guid ownerId, Guid reservationId, CancellationToken cancellationToken);
    Task<OwnerReservationDto> CheckInReservationAsync(Guid ownerId, Guid reservationId, CancellationToken cancellationToken);
    Task<int> ProcessOverdueReservationsAsync(CancellationToken cancellationToken);
}
