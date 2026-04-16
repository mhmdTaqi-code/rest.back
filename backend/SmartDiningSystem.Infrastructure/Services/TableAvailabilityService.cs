using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.TableAvailability;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class TableAvailabilityService : ITableAvailabilityService
{
    private const int BookingExpiryMinutes = 30;

    private readonly AppDbContext _dbContext;

    public TableAvailabilityService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TableAvailabilityStateDto> GetTableStateAsync(Guid tableId, CancellationToken cancellationToken)
    {
        if (tableId == Guid.Empty)
        {
            throw BuildValidationError("Table id is required.", "tableId", "Table id is required.");
        }

        await ExpireOverdueBookingsAsync(cancellationToken);

        var table = await _dbContext.RestaurantTables
            .AsNoTracking()
            .Where(entity => entity.Id == tableId)
            .Select(entity => new TableLookup(
                entity.Id,
                entity.RestaurantId,
                entity.TableNumber,
                entity.IsActive,
                entity.Restaurant != null
                    ? entity.Restaurant.ApprovalStatus
                    : (RestaurantApprovalStatus?)null))
            .FirstOrDefaultAsync(cancellationToken);

        if (table is null || table.ApprovalStatus != RestaurantApprovalStatus.Approved)
        {
            throw new BookingFlowServiceException(
                "Restaurant table was not found.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["tableId"] = ["The selected table was not found."]
                });
        }

        var nowUtc = DateTime.UtcNow;
        var reservationWindowStartUtc = nowUtc.AddMinutes(-BookingExpiryMinutes);

        var activeSession = await _dbContext.TableSessions
            .AsNoTracking()
            .Where(s => s.Status == TableSessionStatus.Active
                     && s.RestaurantTableId == table.Id)
            .OrderByDescending(s => s.OpenedAtUtc)
            .Select(s => new SessionLookup(
                s.Id,
                s.RestaurantId,
                s.RestaurantTableId,
                s.UserId,
                s.BookingId,
                s.OpenedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        var activeBooking = await _dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Confirmed
                     && b.RestaurantTableId == table.Id
                     && b.ReservationTimeUtc >= reservationWindowStartUtc)
            .OrderBy(b => b.ReservationTimeUtc)
            .Select(b => new BookingLookup(
                b.Id,
                b.RestaurantId,
                b.RestaurantTableId,
                b.UserId,
                b.Status,
                b.ReservationTimeUtc))
            .FirstOrDefaultAsync(cancellationToken);

        return BuildState(table, activeSession, activeBooking);
    }

    public async Task<IReadOnlyList<TableAvailabilityStateDto>> GetRestaurantTableStatesAsync(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        if (restaurantId == Guid.Empty)
        {
            throw BuildValidationError("Restaurant id is required.", "restaurantId", "Restaurant id is required.");
        }

        await ExpireOverdueBookingsAsync(cancellationToken);

        var tables = await _dbContext.RestaurantTables
            .AsNoTracking()
            .Where(table => table.RestaurantId == restaurantId)
            .OrderBy(table => table.TableNumber)
            .Select(table => new TableLookup(
                table.Id,
                table.RestaurantId,
                table.TableNumber,
                table.IsActive,
                null))
            .ToListAsync(cancellationToken);

        if (tables.Count == 0)
        {
            return [];
        }

        var tableIds = tables.Select(table => table.Id).ToHashSet();

        var bulkNowUtc = DateTime.UtcNow;
        var bulkReservationWindowStartUtc = bulkNowUtc.AddMinutes(-BookingExpiryMinutes);

        var activeSessions = await _dbContext.TableSessions
            .AsNoTracking()
            .Where(s => s.Status == TableSessionStatus.Active
                     && tableIds.Contains(s.RestaurantTableId))
            .OrderByDescending(s => s.OpenedAtUtc)
            .Select(s => new SessionLookup(
                s.Id,
                s.RestaurantId,
                s.RestaurantTableId,
                s.UserId,
                s.BookingId,
                s.OpenedAtUtc))
            .ToListAsync(cancellationToken);

        var activeBookings = await _dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Confirmed
                     && tableIds.Contains(b.RestaurantTableId)
                     && b.ReservationTimeUtc >= bulkReservationWindowStartUtc)
            .OrderBy(b => b.ReservationTimeUtc)
            .Select(b => new BookingLookup(
                b.Id,
                b.RestaurantId,
                b.RestaurantTableId,
                b.UserId,
                b.Status,
                b.ReservationTimeUtc))
            .ToListAsync(cancellationToken);

        return tables
            .Select(table => BuildState(
                table,
                activeSessions.FirstOrDefault(session => session.RestaurantTableId == table.Id),
                activeBookings.FirstOrDefault(booking => booking.RestaurantTableId == table.Id)))
            .ToList();
    }

    public async Task<int> ExpireOverdueBookingsAsync(CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var noShowCutoffUtc = nowUtc.AddMinutes(-BookingExpiryMinutes);
        var overdueBookings = await _dbContext.Bookings
            .Where(booking =>
                booking.Status == BookingStatus.Confirmed &&
                booking.ReservationTimeUtc < noShowCutoffUtc)
            .ToListAsync(cancellationToken);

        if (overdueBookings.Count == 0)
        {
            return 0;
        }

        foreach (var booking in overdueBookings)
        {
            booking.Status = BookingStatus.NoShow;
            booking.NoShowMarkedAtUtc ??= nowUtc;
            booking.UpdatedAtUtc = nowUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return overdueBookings.Count;
    }

    private static TableAvailabilityStateDto BuildState(
        TableLookup table,
        SessionLookup? activeSession,
        BookingLookup? activeBooking)
    {
        var isOrderingEnabled = table.IsActive;
        var status = ResolveStatus(table.IsActive, isOrderingEnabled, activeSession is not null, activeBooking is not null);

        return new TableAvailabilityStateDto
        {
            TableId = table.Id,
            RestaurantId = table.RestaurantId,
            TableNumber = table.TableNumber,
            IsTableActive = table.IsActive,
            IsOrderingEnabled = isOrderingEnabled,
            Status = status,
            IsAvailable = status == "Available",
            HasActiveSession = activeSession is not null,
            ActiveSessionId = activeSession?.Id,
            ActiveSessionUserId = activeSession?.UserId,
            ActiveSessionBookingId = activeSession?.BookingId,
            ActiveSessionOpenedAtUtc = activeSession?.OpenedAtUtc,
            HasActiveBooking = activeBooking is not null,
            ActiveBookingId = activeBooking?.Id,
            ActiveBookingUserId = activeBooking?.UserId,
            ActiveBookingStatus = activeBooking?.Status.ToString(),
            ReservationTimeUtc = activeBooking?.ReservationTimeUtc
        };
    }

    private static string ResolveStatus(
        bool isTableActive,
        bool isOrderingEnabled,
        bool hasActiveSession,
        bool hasActiveBooking)
    {
        if (!isTableActive || !isOrderingEnabled)
        {
            return "OutOfService";
        }

        if (hasActiveSession)
        {
            return "Occupied";
        }

        if (hasActiveBooking)
        {
            return "Reserved";
        }

        return "Available";
    }

    private static BookingFlowServiceException BuildValidationError(string message, string key, string error)
    {
        return new BookingFlowServiceException(
            message,
            StatusCodes.Status400BadRequest,
            new Dictionary<string, string[]>
            {
                [key] = [error]
            });
    }

    private sealed record TableLookup(
        Guid Id,
        Guid RestaurantId,
        int TableNumber,
        bool IsActive,
        RestaurantApprovalStatus? ApprovalStatus);

    private sealed record SessionLookup(
        Guid Id,
        Guid RestaurantId,
        Guid RestaurantTableId,
        Guid? UserId,
        Guid? BookingId,
        DateTime OpenedAtUtc);

    private sealed record BookingLookup(
        Guid Id,
        Guid RestaurantId,
        Guid RestaurantTableId,
        Guid UserId,
        BookingStatus Status,
        DateTime ReservationTimeUtc);
}
