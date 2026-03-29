using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.Reservations;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class TableReservationService : ITableReservationService
{
    private const int GracePeriodMinutes = 30;
    private const decimal MinimumDepositAmount = 5000m;
    private static readonly ReservationStatus[] BlockingStatuses =
    [
        ReservationStatus.PendingPayment,
        ReservationStatus.Confirmed,
        ReservationStatus.CheckedIn
    ];

    private readonly AppDbContext _dbContext;

    public TableReservationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReservationDto> CreateReservationAsync(
        Guid userId,
        CreateReservationRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await ProcessOverdueReservationsAsync(cancellationToken);

        var user = await _dbContext.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(account => account.Id == userId && account.IsActive, cancellationToken);

        if (user is null)
        {
            throw new TableReservationServiceException(
                "User account was not found.",
                StatusCodes.Status404NotFound);
        }

        var table = await _dbContext.RestaurantTables
            .Include(entity => entity.Restaurant)
            .ThenInclude(restaurant => restaurant!.Ratings)
            .FirstOrDefaultAsync(entity => entity.Id == request.RestaurantTableId, cancellationToken);

        if (table is null)
        {
            throw BuildValidationException(
                "Restaurant table was not found.",
                nameof(request.RestaurantTableId),
                "Please select a valid restaurant table.");
        }

        if (table.RestaurantId != request.RestaurantId)
        {
            throw BuildValidationException(
                "The selected table does not belong to the selected restaurant.",
                nameof(request.RestaurantTableId),
                "Please choose a table that belongs to the requested restaurant.");
        }

        if (!table.IsActive)
        {
            throw BuildValidationException(
                "The selected table is not active.",
                nameof(request.RestaurantTableId),
                "Only active tables can be reserved.");
        }

        var restaurant = table.Restaurant;
        if (restaurant is null ||
            restaurant.ApprovalStatus != RestaurantApprovalStatus.Approved)
        {
            throw new TableReservationServiceException(
                "Reservations are available only for approved restaurants.",
                StatusCodes.Status404NotFound);
        }

        var nowUtc = DateTime.UtcNow;
        var gracePeriodEndsAtUtc = request.ReservationStartUtc.AddMinutes(GracePeriodMinutes);

        if (request.ReservationStartUtc <= nowUtc)
        {
            throw BuildValidationException(
                "Reservation start time must be in the future.",
                nameof(request.ReservationStartUtc),
                "Please choose a future reservation start time.");
        }

        await EnsureNoOverlapAsync(
            request.RestaurantTableId,
            request.ReservationStartUtc,
            gracePeriodEndsAtUtc,
            excludeReservationId: null,
            cancellationToken);

        var reservation = new TableReservation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RestaurantId = restaurant.Id,
            RestaurantTableId = table.Id,
            ReservationStartUtc = request.ReservationStartUtc,
            ReservationEndUtc = gracePeriodEndsAtUtc,
            GuestCount = request.GuestCount,
            DepositAmount = MinimumDepositAmount,
            IsDepositPaid = false,
            Status = ReservationStatus.PendingPayment,
            GracePeriodEndsAtUtc = gracePeriodEndsAtUtc,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

        _dbContext.TableReservations.Add(reservation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapReservationDto(
            reservation,
            restaurant.Name,
            CalculateAverageRating(restaurant),
            CalculateTotalRatingsCount(restaurant),
            table.TableNumber);
    }

    public async Task<IReadOnlyList<ReservationDto>> GetMyReservationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        await ProcessOverdueReservationsAsync(cancellationToken);

        return await _dbContext.TableReservations
            .AsNoTracking()
            .Where(reservation => reservation.UserId == userId)
            .OrderByDescending(reservation => reservation.ReservationStartUtc)
            .Select(reservation => new ReservationDto
            {
                ReservationId = reservation.Id,
                UserId = reservation.UserId,
                RestaurantId = reservation.RestaurantId,
                RestaurantName = reservation.Restaurant != null ? reservation.Restaurant.Name : string.Empty,
                AverageRating = reservation.Restaurant != null
                    ? Math.Round(reservation.Restaurant.Ratings.Select(rating => (double?)rating.Stars).Average() ?? 0d, 2)
                    : 0d,
                TotalRatingsCount = reservation.Restaurant != null
                    ? reservation.Restaurant.Ratings.Count()
                    : 0,
                RestaurantTableId = reservation.RestaurantTableId,
                TableNumber = reservation.RestaurantTable != null ? reservation.RestaurantTable.TableNumber : 0,
                ReservationStartUtc = reservation.ReservationStartUtc,
                ReservationEndUtc = reservation.ReservationEndUtc,
                GracePeriodEndsAtUtc = reservation.GracePeriodEndsAtUtc,
                GuestCount = reservation.GuestCount,
                DepositAmount = reservation.DepositAmount,
                IsDepositPaid = reservation.IsDepositPaid,
                Status = reservation.Status.ToString(),
                CreatedAtUtc = reservation.CreatedAtUtc,
                UpdatedAtUtc = reservation.UpdatedAtUtc,
                DepositPaidAtUtc = reservation.DepositPaidAtUtc,
                ConfirmedAtUtc = reservation.ConfirmedAtUtc,
                CheckedInAtUtc = reservation.CheckedInAtUtc,
                CancelledAtUtc = reservation.CancelledAtUtc,
                CancellationReason = reservation.CancellationReason,
                NoShowMarkedAtUtc = reservation.NoShowMarkedAtUtc,
                DepositForfeitedAtUtc = reservation.DepositForfeitedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ReservationDto> GetMyReservationAsync(
        Guid userId,
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        await ProcessOverdueReservationsAsync(cancellationToken);

        var reservation = await _dbContext.TableReservations
            .AsNoTracking()
            .Include(entity => entity.Restaurant)
            .ThenInclude(restaurant => restaurant!.Ratings)
            .Include(entity => entity.RestaurantTable)
            .FirstOrDefaultAsync(
                entity => entity.Id == reservationId && entity.UserId == userId,
                cancellationToken);

        if (reservation is null)
        {
            throw new TableReservationServiceException(
                "Reservation was not found for this user.",
                StatusCodes.Status404NotFound);
        }

        return MapReservationDto(
            reservation,
            reservation.Restaurant?.Name ?? string.Empty,
            CalculateAverageRating(reservation.Restaurant),
            CalculateTotalRatingsCount(reservation.Restaurant),
            reservation.RestaurantTable?.TableNumber ?? 0);
    }

    public async Task<ReservationDto> CancelReservationAsync(
        Guid userId,
        Guid reservationId,
        CancelReservationRequestDto? request,
        CancellationToken cancellationToken)
    {
        await ProcessOverdueReservationsAsync(cancellationToken);

        var reservation = await _dbContext.TableReservations
            .Include(entity => entity.Restaurant)
            .ThenInclude(restaurant => restaurant!.Ratings)
            .Include(entity => entity.RestaurantTable)
            .FirstOrDefaultAsync(
                entity => entity.Id == reservationId && entity.UserId == userId,
                cancellationToken);

        if (reservation is null)
        {
            throw new TableReservationServiceException(
                "Reservation was not found for this user.",
                StatusCodes.Status404NotFound);
        }

        if (reservation.Status is not ReservationStatus.PendingPayment and not ReservationStatus.Confirmed)
        {
            throw new TableReservationServiceException(
                "Only pending payment or confirmed reservations can be cancelled.",
                StatusCodes.Status409Conflict);
        }

        if (reservation.ReservationStartUtc <= DateTime.UtcNow)
        {
            throw new TableReservationServiceException(
                "Reservations can only be cancelled before the reservation start time.",
                StatusCodes.Status409Conflict);
        }

        var nowUtc = DateTime.UtcNow;
        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelledAtUtc = nowUtc;
        reservation.CancellationReason = NormalizeOptionalText(request?.CancellationReason);
        reservation.UpdatedAtUtc = nowUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapReservationDto(
            reservation,
            reservation.Restaurant?.Name ?? string.Empty,
            CalculateAverageRating(reservation.Restaurant),
            CalculateTotalRatingsCount(reservation.Restaurant),
            reservation.RestaurantTable?.TableNumber ?? 0);
    }

    public async Task<ReservationDto> MarkDepositPaidAsync(
        Guid userId,
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        await ProcessOverdueReservationsAsync(cancellationToken);

        var reservation = await _dbContext.TableReservations
            .Include(entity => entity.Restaurant)
            .ThenInclude(restaurant => restaurant!.Ratings)
            .Include(entity => entity.RestaurantTable)
            .FirstOrDefaultAsync(
                entity => entity.Id == reservationId && entity.UserId == userId,
                cancellationToken);

        if (reservation is null)
        {
            throw new TableReservationServiceException(
                "Reservation was not found for this user.",
                StatusCodes.Status404NotFound);
        }

        if (reservation.Status != ReservationStatus.PendingPayment)
        {
            throw new TableReservationServiceException(
                "Only pending payment reservations can be marked as paid.",
                StatusCodes.Status409Conflict);
        }

        var nowUtc = DateTime.UtcNow;
        if (reservation.ReservationStartUtc <= nowUtc)
        {
            throw new TableReservationServiceException(
                "The reservation can no longer be confirmed because the start time has passed.",
                StatusCodes.Status409Conflict);
        }

        await EnsureNoOverlapAsync(
            reservation.RestaurantTableId,
            reservation.ReservationStartUtc,
            reservation.ReservationEndUtc,
            reservation.Id,
            cancellationToken);

        reservation.IsDepositPaid = true;
        reservation.DepositPaidAtUtc = nowUtc;
        reservation.ConfirmedAtUtc = nowUtc;
        reservation.GracePeriodEndsAtUtc ??= reservation.ReservationStartUtc.AddMinutes(GracePeriodMinutes);
        reservation.Status = ReservationStatus.Confirmed;
        reservation.UpdatedAtUtc = nowUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapReservationDto(
            reservation,
            reservation.Restaurant?.Name ?? string.Empty,
            CalculateAverageRating(reservation.Restaurant),
            CalculateTotalRatingsCount(reservation.Restaurant),
            reservation.RestaurantTable?.TableNumber ?? 0);
    }

    public async Task<IReadOnlyList<OwnerReservationDto>> GetOwnerReservationsAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        await ProcessOverdueReservationsAsync(cancellationToken);

        return await _dbContext.TableReservations
            .AsNoTracking()
            .Where(reservation => reservation.Restaurant != null && reservation.Restaurant.OwnerId == ownerId)
            .OrderByDescending(reservation => reservation.ReservationStartUtc)
            .Select(reservation => new OwnerReservationDto
            {
                ReservationId = reservation.Id,
                UserId = reservation.UserId,
                UserFullName = reservation.User != null ? reservation.User.FullName : string.Empty,
                UserPhoneNumber = reservation.User != null ? reservation.User.PhoneNumber : string.Empty,
                RestaurantId = reservation.RestaurantId,
                RestaurantName = reservation.Restaurant != null ? reservation.Restaurant.Name : string.Empty,
                AverageRating = reservation.Restaurant != null
                    ? Math.Round(reservation.Restaurant.Ratings.Select(rating => (double?)rating.Stars).Average() ?? 0d, 2)
                    : 0d,
                TotalRatingsCount = reservation.Restaurant != null
                    ? reservation.Restaurant.Ratings.Count()
                    : 0,
                RestaurantTableId = reservation.RestaurantTableId,
                TableNumber = reservation.RestaurantTable != null ? reservation.RestaurantTable.TableNumber : 0,
                ReservationStartUtc = reservation.ReservationStartUtc,
                ReservationEndUtc = reservation.ReservationEndUtc,
                GracePeriodEndsAtUtc = reservation.GracePeriodEndsAtUtc,
                GuestCount = reservation.GuestCount,
                DepositAmount = reservation.DepositAmount,
                IsDepositPaid = reservation.IsDepositPaid,
                Status = reservation.Status.ToString(),
                CreatedAtUtc = reservation.CreatedAtUtc,
                UpdatedAtUtc = reservation.UpdatedAtUtc,
                DepositPaidAtUtc = reservation.DepositPaidAtUtc,
                ConfirmedAtUtc = reservation.ConfirmedAtUtc,
                CheckedInAtUtc = reservation.CheckedInAtUtc,
                CancelledAtUtc = reservation.CancelledAtUtc,
                CancellationReason = reservation.CancellationReason,
                NoShowMarkedAtUtc = reservation.NoShowMarkedAtUtc,
                DepositForfeitedAtUtc = reservation.DepositForfeitedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OwnerReservationDto> GetOwnerReservationAsync(
        Guid ownerId,
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        await ProcessOverdueReservationsAsync(cancellationToken);

        var reservation = await _dbContext.TableReservations
            .AsNoTracking()
            .Include(entity => entity.User)
            .Include(entity => entity.Restaurant)
            .ThenInclude(restaurant => restaurant!.Ratings)
            .Include(entity => entity.RestaurantTable)
            .FirstOrDefaultAsync(
                entity => entity.Id == reservationId && entity.Restaurant != null && entity.Restaurant.OwnerId == ownerId,
                cancellationToken);

        if (reservation is null)
        {
            throw new TableReservationServiceException(
                "Reservation was not found for this restaurant owner.",
                StatusCodes.Status404NotFound);
        }

        return MapOwnerReservationDto(
            reservation,
            reservation.Restaurant?.Name ?? string.Empty,
            CalculateAverageRating(reservation.Restaurant),
            CalculateTotalRatingsCount(reservation.Restaurant),
            reservation.RestaurantTable?.TableNumber ?? 0);
    }

    public async Task<OwnerReservationDto> CheckInReservationAsync(
        Guid ownerId,
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        await ProcessOverdueReservationsAsync(cancellationToken);

        var reservation = await _dbContext.TableReservations
            .Include(entity => entity.User)
            .Include(entity => entity.Restaurant)
            .ThenInclude(restaurant => restaurant!.Ratings)
            .Include(entity => entity.RestaurantTable)
            .FirstOrDefaultAsync(
                entity => entity.Id == reservationId && entity.Restaurant != null && entity.Restaurant.OwnerId == ownerId,
                cancellationToken);

        if (reservation is null)
        {
            throw new TableReservationServiceException(
                "Reservation was not found for this restaurant owner.",
                StatusCodes.Status404NotFound);
        }

        if (reservation.Status != ReservationStatus.Confirmed || !reservation.IsDepositPaid)
        {
            throw new TableReservationServiceException(
                "Only confirmed reservations with a paid deposit can be checked in.",
                StatusCodes.Status409Conflict);
        }

        var nowUtc = DateTime.UtcNow;
        var gracePeriodEndsAtUtc = reservation.GracePeriodEndsAtUtc ?? reservation.ReservationStartUtc.AddMinutes(GracePeriodMinutes);
        if (gracePeriodEndsAtUtc < nowUtc)
        {
            await ProcessOverdueReservationsAsync(cancellationToken);

            throw new TableReservationServiceException(
                "The reservation can no longer be checked in because the grace period has expired.",
                StatusCodes.Status409Conflict);
        }

        reservation.Status = ReservationStatus.CheckedIn;
        reservation.CheckedInAtUtc = nowUtc;
        reservation.UpdatedAtUtc = nowUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapOwnerReservationDto(
            reservation,
            reservation.Restaurant?.Name ?? string.Empty,
            CalculateAverageRating(reservation.Restaurant),
            CalculateTotalRatingsCount(reservation.Restaurant),
            reservation.RestaurantTable?.TableNumber ?? 0);
    }

    public async Task<int> ProcessOverdueReservationsAsync(CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        var expiredPendingReservations = await _dbContext.TableReservations
            .Where(reservation =>
                reservation.Status == ReservationStatus.PendingPayment &&
                reservation.ReservationStartUtc <= nowUtc)
            .ToListAsync(cancellationToken);

        var overdueConfirmedReservations = await _dbContext.TableReservations
            .Where(reservation =>
                reservation.Status == ReservationStatus.Confirmed &&
                ((reservation.GracePeriodEndsAtUtc.HasValue && reservation.GracePeriodEndsAtUtc <= nowUtc) ||
                 (!reservation.GracePeriodEndsAtUtc.HasValue && reservation.ReservationStartUtc <= nowUtc.AddMinutes(-GracePeriodMinutes))))
            .ToListAsync(cancellationToken);

        foreach (var reservation in expiredPendingReservations)
        {
            reservation.Status = ReservationStatus.Cancelled;
            reservation.CancelledAtUtc = nowUtc;
            reservation.CancellationReason ??= "Reservation was cancelled because the deposit was not paid before the start time.";
            reservation.UpdatedAtUtc = nowUtc;
        }

        foreach (var reservation in overdueConfirmedReservations)
        {
            reservation.Status = ReservationStatus.NoShow;
            reservation.NoShowMarkedAtUtc = nowUtc;
            reservation.CancelledAtUtc = nowUtc;
            reservation.CancellationReason ??= "Reservation became a no-show because the customer did not check in within 30 minutes after the reservation start time.";
            reservation.DepositForfeitedAtUtc = reservation.IsDepositPaid ? nowUtc : reservation.DepositForfeitedAtUtc;
            reservation.UpdatedAtUtc = nowUtc;
        }

        if (expiredPendingReservations.Count == 0 && overdueConfirmedReservations.Count == 0)
        {
            return 0;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return expiredPendingReservations.Count + overdueConfirmedReservations.Count;
    }

    private async Task EnsureNoOverlapAsync(
        Guid tableId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeReservationId,
        CancellationToken cancellationToken)
    {
        var hasOverlap = await _dbContext.TableReservations
            .AnyAsync(
                reservation =>
                    reservation.RestaurantTableId == tableId &&
                    BlockingStatuses.Contains(reservation.Status) &&
                    (!excludeReservationId.HasValue || reservation.Id != excludeReservationId.Value) &&
                    startUtc < reservation.ReservationEndUtc &&
                    endUtc > reservation.ReservationStartUtc,
                cancellationToken);

        if (hasOverlap)
        {
            throw new TableReservationServiceException(
                "The selected table already has an active reservation in the requested time range.",
                StatusCodes.Status409Conflict,
                new Dictionary<string, string[]>
                {
                    ["reservationWindow"] = ["Please choose a different table or reservation time."]
                });
        }
    }

    private static ReservationDto MapReservationDto(
        TableReservation reservation,
        string restaurantName,
        double averageRating,
        int totalRatingsCount,
        int tableNumber)
    {
        return new ReservationDto
        {
            ReservationId = reservation.Id,
            UserId = reservation.UserId,
            RestaurantId = reservation.RestaurantId,
            RestaurantName = restaurantName,
            AverageRating = averageRating,
            TotalRatingsCount = totalRatingsCount,
            RestaurantTableId = reservation.RestaurantTableId,
            TableNumber = tableNumber,
            ReservationStartUtc = reservation.ReservationStartUtc,
            ReservationEndUtc = reservation.ReservationEndUtc,
            GracePeriodEndsAtUtc = reservation.GracePeriodEndsAtUtc,
            GuestCount = reservation.GuestCount,
            DepositAmount = reservation.DepositAmount,
            IsDepositPaid = reservation.IsDepositPaid,
            Status = reservation.Status.ToString(),
            CreatedAtUtc = reservation.CreatedAtUtc,
            UpdatedAtUtc = reservation.UpdatedAtUtc,
            DepositPaidAtUtc = reservation.DepositPaidAtUtc,
            ConfirmedAtUtc = reservation.ConfirmedAtUtc,
            CheckedInAtUtc = reservation.CheckedInAtUtc,
            CancelledAtUtc = reservation.CancelledAtUtc,
            CancellationReason = reservation.CancellationReason,
            NoShowMarkedAtUtc = reservation.NoShowMarkedAtUtc,
            DepositForfeitedAtUtc = reservation.DepositForfeitedAtUtc
        };
    }

    private static OwnerReservationDto MapOwnerReservationDto(
        TableReservation reservation,
        string restaurantName,
        double averageRating,
        int totalRatingsCount,
        int tableNumber)
    {
        return new OwnerReservationDto
        {
            ReservationId = reservation.Id,
            UserId = reservation.UserId,
            UserFullName = reservation.User?.FullName ?? string.Empty,
            UserPhoneNumber = reservation.User?.PhoneNumber ?? string.Empty,
            RestaurantId = reservation.RestaurantId,
            RestaurantName = restaurantName,
            AverageRating = averageRating,
            TotalRatingsCount = totalRatingsCount,
            RestaurantTableId = reservation.RestaurantTableId,
            TableNumber = tableNumber,
            ReservationStartUtc = reservation.ReservationStartUtc,
            ReservationEndUtc = reservation.ReservationEndUtc,
            GracePeriodEndsAtUtc = reservation.GracePeriodEndsAtUtc,
            GuestCount = reservation.GuestCount,
            DepositAmount = reservation.DepositAmount,
            IsDepositPaid = reservation.IsDepositPaid,
            Status = reservation.Status.ToString(),
            CreatedAtUtc = reservation.CreatedAtUtc,
            UpdatedAtUtc = reservation.UpdatedAtUtc,
            DepositPaidAtUtc = reservation.DepositPaidAtUtc,
            ConfirmedAtUtc = reservation.ConfirmedAtUtc,
            CheckedInAtUtc = reservation.CheckedInAtUtc,
            CancelledAtUtc = reservation.CancelledAtUtc,
            CancellationReason = reservation.CancellationReason,
            NoShowMarkedAtUtc = reservation.NoShowMarkedAtUtc,
            DepositForfeitedAtUtc = reservation.DepositForfeitedAtUtc
        };
    }

    private static double CalculateAverageRating(Restaurant? restaurant)
    {
        return restaurant is null
            ? 0d
            : Math.Round(restaurant.Ratings.Select(rating => (double)rating.Stars).DefaultIfEmpty().Average(), 2);
    }

    private static int CalculateTotalRatingsCount(Restaurant? restaurant)
    {
        return restaurant?.Ratings.Count ?? 0;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static TableReservationServiceException BuildValidationException(
        string message,
        string key,
        string error)
    {
        return new TableReservationServiceException(
            message,
            StatusCodes.Status400BadRequest,
            new Dictionary<string, string[]>
            {
                [key] = [error]
            });
    }
}
