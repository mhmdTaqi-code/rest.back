using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.TableAccess;
using SmartDiningSystem.Application.DTOs.TableOrdering;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class TableAccessFlowService : ITableAccessFlowService
{
    private const int BookingExpiryMinutes = 30;

    private readonly AppDbContext _dbContext;
    private readonly BookingService _bookingService;
    private readonly ITableSessionOrderService _tableSessionOrderService;

    public TableAccessFlowService(
        AppDbContext dbContext,
        BookingService bookingService,
        ITableSessionOrderService tableSessionOrderService)
    {
        _dbContext = dbContext;
        _bookingService = bookingService;
        _tableSessionOrderService = tableSessionOrderService;
    }

    public async Task<TableAccessScanResponseDto> ProcessScanAsync(
        Guid? userId,
        TableAccessScanRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.TableId == Guid.Empty)
        {
            throw BuildValidationError(
                "Table id is required.",
                StatusCodes.Status400BadRequest,
                "tableId",
                "Table id is required.");
        }

        await _bookingService.ExpireOverdueBookingsAsync(cancellationToken);

        var table = await LoadTableAsync(request.TableId, cancellationToken);
        var requestedItems = request.Items;
        var hasItems = requestedItems is { Count: > 0 };

        if (!table.IsActive)
        {
            return CreateResponse(
                table,
                bookingId: null,
                hasBooking: false,
                isBookingOwner: false,
                isCheckedIn: false,
                checkInPerformed: false,
                requiresLogin: false,
                isBlocked: true,
                blockReason: "OutOfService",
                canOrder: false,
                orderCreated: false,
                order: null,
                message: "This table is currently out of service.");
        }

        var activeSession = await LoadActiveSessionAsync(table.Id, cancellationToken);
        if (activeSession is not null)
        {
            if (!userId.HasValue || activeSession.UserId != userId.Value)
            {
                return CreateResponse(
                    table,
                    bookingId: null,
                    hasBooking: activeSession.BookingId.HasValue,
                    isBookingOwner: false,
                    isCheckedIn: false,
                    checkInPerformed: false,
                    requiresLogin: false,
                    isBlocked: true,
                    blockReason: "Occupied",
                    canOrder: false,
                    orderCreated: false,
                    order: null,
                    message: "This table is currently occupied.");
            }

            if (!hasItems)
            {
                return CreateResponse(
                    table,
                    bookingId: activeSession.BookingId,
                    hasBooking: activeSession.BookingId.HasValue,
                    isBookingOwner: activeSession.BookingId.HasValue,
                    isCheckedIn: activeSession.BookingId.HasValue,
                    checkInPerformed: false,
                    requiresLogin: false,
                    isBlocked: false,
                    blockReason: null,
                    canOrder: true,
                    orderCreated: false,
                    order: null,
                    message: "You already have access to this table and can order now.");
            }

            await EnsureActiveUserAsync(userId.Value, cancellationToken);
            var existingSessionOrder = await SubmitOrderAsync(userId.Value, activeSession.Id, requestedItems!, cancellationToken);

            return CreateResponse(
                table,
                bookingId: activeSession.BookingId,
                hasBooking: activeSession.BookingId.HasValue,
                isBookingOwner: activeSession.BookingId.HasValue,
                isCheckedIn: activeSession.BookingId.HasValue,
                checkInPerformed: false,
                requiresLogin: false,
                isBlocked: false,
                blockReason: null,
                canOrder: true,
                orderCreated: true,
                order: MapOrder(existingSessionOrder),
                message: "Order created successfully.");
        }

        var currentBooking = await LoadCurrentConfirmedBookingAsync(table.Id, cancellationToken);
        if (currentBooking is not null)
        {
            if (!userId.HasValue)
            {
                return CreateResponse(
                    table,
                    bookingId: null,
                    hasBooking: true,
                    isBookingOwner: false,
                    isCheckedIn: false,
                    checkInPerformed: false,
                    requiresLogin: true,
                    isBlocked: false,
                    blockReason: null,
                    canOrder: false,
                    orderCreated: false,
                    order: null,
                    message: "Log in to verify this booking and continue.");
            }

            if (currentBooking.UserId != userId.Value)
            {
                return CreateResponse(
                    table,
                    bookingId: null,
                    hasBooking: true,
                    isBookingOwner: false,
                    isCheckedIn: false,
                    checkInPerformed: false,
                    requiresLogin: false,
                    isBlocked: true,
                    blockReason: "Reserved",
                    canOrder: false,
                    orderCreated: false,
                    order: null,
                    message: "This table is reserved for another booking.");
            }

            var checkInResult = await _bookingService.CheckInAsync(userId.Value, currentBooking.Id, cancellationToken);

            if (!hasItems)
            {
                return CreateResponse(
                    table,
                    bookingId: currentBooking.Id,
                    hasBooking: true,
                    isBookingOwner: true,
                    isCheckedIn: true,
                    checkInPerformed: true,
                    requiresLogin: false,
                    isBlocked: false,
                    blockReason: null,
                    canOrder: true,
                    orderCreated: false,
                    order: null,
                    message: "Booking checked in successfully. You can order now.");
            }

            await EnsureActiveUserAsync(userId.Value, cancellationToken);
            var checkedInOrder = await SubmitOrderAsync(userId.Value, checkInResult.SessionId, requestedItems!, cancellationToken);

            return CreateResponse(
                table,
                bookingId: currentBooking.Id,
                hasBooking: true,
                isBookingOwner: true,
                isCheckedIn: true,
                checkInPerformed: true,
                requiresLogin: false,
                isBlocked: false,
                blockReason: null,
                canOrder: true,
                orderCreated: true,
                order: MapOrder(checkedInOrder),
                message: "Booking checked in and order created successfully.");
        }

        var upcomingBooking = await LoadUpcomingConfirmedBookingAsync(table.Id, cancellationToken);
        if (upcomingBooking is not null)
        {
            if (!userId.HasValue)
            {
                return CreateResponse(
                    table,
                    bookingId: null,
                    hasBooking: true,
                    isBookingOwner: false,
                    isCheckedIn: false,
                    checkInPerformed: false,
                    requiresLogin: true,
                    isBlocked: false,
                    blockReason: null,
                    canOrder: false,
                    orderCreated: false,
                    order: null,
                    message: "Log in to verify whether this upcoming booking belongs to you.");
            }

            if (upcomingBooking.UserId != userId.Value)
            {
                return CreateResponse(
                    table,
                    bookingId: null,
                    hasBooking: true,
                    isBookingOwner: false,
                    isCheckedIn: false,
                    checkInPerformed: false,
                    requiresLogin: false,
                    isBlocked: true,
                    blockReason: "Reserved",
                    canOrder: false,
                    orderCreated: false,
                    order: null,
                    message: "This table is reserved for another booking.");
            }

            return CreateResponse(
                table,
                bookingId: upcomingBooking.Id,
                hasBooking: true,
                isBookingOwner: true,
                isCheckedIn: false,
                checkInPerformed: false,
                requiresLogin: false,
                isBlocked: false,
                blockReason: null,
                canOrder: false,
                orderCreated: false,
                order: null,
                message: "Your booking exists for this table, but ordering and check-in are not available yet.");
        }

        if (hasItems)
        {
            if (!userId.HasValue)
            {
                return CreateResponse(
                    table,
                    bookingId: null,
                    hasBooking: false,
                    isBookingOwner: false,
                    isCheckedIn: false,
                    checkInPerformed: false,
                    requiresLogin: true,
                    isBlocked: false,
                    blockReason: null,
                    canOrder: false,
                    orderCreated: false,
                    order: null,
                    message: "Log in to place an order for this table.");
            }

            await EnsureActiveUserAsync(userId.Value, cancellationToken);
            var directSession = await GetOrCreateDirectSessionAsync(table, userId.Value, cancellationToken);
            var directOrder = await SubmitOrderAsync(userId.Value, directSession.Id, requestedItems!, cancellationToken);

            return CreateResponse(
                table,
                bookingId: null,
                hasBooking: false,
                isBookingOwner: false,
                isCheckedIn: false,
                checkInPerformed: false,
                requiresLogin: false,
                isBlocked: false,
                blockReason: null,
                canOrder: true,
                orderCreated: true,
                order: MapOrder(directOrder),
                message: "Order created successfully.");
        }

        return CreateResponse(
            table,
            bookingId: null,
            hasBooking: false,
            isBookingOwner: false,
            isCheckedIn: false,
            checkInPerformed: false,
            requiresLogin: false,
            isBlocked: false,
            blockReason: null,
            canOrder: true,
            orderCreated: false,
            order: null,
            message: "This table is available for access and ordering.");
    }

    private async Task<TableLookup> LoadTableAsync(Guid tableId, CancellationToken cancellationToken)
    {
        var table = await _dbContext.RestaurantTables
            .AsNoTracking()
            .Where(entity => entity.Id == tableId)
            .Select(entity => new
            {
                entity.Id,
                entity.RestaurantId,
                entity.TableNumber,
                entity.IsActive,
                ApprovalStatus = entity.Restaurant != null
                    ? entity.Restaurant.ApprovalStatus
                    : (RestaurantApprovalStatus?)null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (table is null || table.ApprovalStatus is null || table.ApprovalStatus != RestaurantApprovalStatus.Approved)
        {
            throw new BookingFlowServiceException(
                "Restaurant table was not found.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["tableId"] = ["The selected table was not found."]
                });
        }

        return new TableLookup(table.Id, table.RestaurantId, table.TableNumber, table.IsActive);
    }

    private async Task<SessionLookup?> LoadActiveSessionAsync(Guid tableId, CancellationToken cancellationToken)
    {
        return await _dbContext.TableSessions
            .AsNoTracking()
            .Where(session => session.RestaurantTableId == tableId && session.Status == TableSessionStatus.Active)
            .OrderByDescending(session => session.OpenedAtUtc)
            .Select(session => new SessionLookup(session.Id, session.UserId, session.BookingId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<BookingLookup?> LoadCurrentConfirmedBookingAsync(Guid tableId, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var reservationWindowStartUtc = nowUtc.AddMinutes(-BookingExpiryMinutes);

        return await _dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.RestaurantTableId == tableId &&
                booking.Status == BookingStatus.Confirmed &&
                booking.ReservationTimeUtc >= reservationWindowStartUtc &&
                booking.ReservationTimeUtc <= nowUtc)
            .OrderBy(booking => booking.ReservationTimeUtc)
            .Select(booking => new BookingLookup(booking.Id, booking.UserId, booking.ReservationTimeUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<BookingLookup?> LoadUpcomingConfirmedBookingAsync(Guid tableId, CancellationToken cancellationToken)
    {
        return await _dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.RestaurantTableId == tableId &&
                booking.Status == BookingStatus.Confirmed &&
                booking.ReservationTimeUtc > DateTime.UtcNow)
            .OrderBy(booking => booking.ReservationTimeUtc)
            .Select(booking => new BookingLookup(booking.Id, booking.UserId, booking.ReservationTimeUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task EnsureActiveUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var isActiveUser = await _dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId && user.IsActive, cancellationToken);

        if (!isActiveUser)
        {
            throw new BookingFlowServiceException(
                "Authenticated user account was not found.",
                StatusCodes.Status401Unauthorized);
        }
    }

    private async Task<TableSession> GetOrCreateDirectSessionAsync(
        TableLookup table,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var existingSession = await _dbContext.TableSessions
            .FirstOrDefaultAsync(
                session => session.RestaurantTableId == table.Id && session.Status == TableSessionStatus.Active,
                cancellationToken);

        if (existingSession is not null)
        {
            if (!existingSession.UserId.HasValue || existingSession.UserId.Value != userId)
            {
                throw new BookingFlowServiceException(
                    "This table is already occupied by another active session.",
                    StatusCodes.Status409Conflict,
                    new Dictionary<string, string[]>
                    {
                        ["tableId"] = ["This table cannot be used until the active session is completed."]
                    });
            }

            return existingSession;
        }

        var session = new TableSession
        {
            Id = Guid.NewGuid(),
            RestaurantId = table.RestaurantId,
            RestaurantTableId = table.Id,
            UserId = userId,
            Status = TableSessionStatus.Active,
            OpenedAtUtc = DateTime.UtcNow
        };

        _dbContext.TableSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return session;
    }

    private async Task<SubmittedTableOrderResponseDto> SubmitOrderAsync(
        Guid userId,
        Guid sessionId,
        IReadOnlyList<SubmitTableOrderItemRequestDto> items,
        CancellationToken cancellationToken)
    {
        return await _tableSessionOrderService.SubmitOrderAsync(
            userId,
            sessionId,
            new SubmitTableOrderRequestDto
            {
                Items = items.ToList()
            },
            cancellationToken);
    }

    private static TableAccessOrderSummaryDto MapOrder(SubmittedTableOrderResponseDto order)
    {
        return new TableAccessOrderSummaryDto
        {
            OrderId = order.OrderId,
            TableNumber = order.TableNumber,
            OrderName = order.OrderName,
            Status = order.Status,
            ItemCount = order.ItemCount,
            TotalAmount = order.TotalAmount,
            CreatedAtUtc = order.CreatedAtUtc
        };
    }

    private static TableAccessScanResponseDto CreateResponse(
        TableLookup table,
        Guid? bookingId,
        bool hasBooking,
        bool isBookingOwner,
        bool isCheckedIn,
        bool checkInPerformed,
        bool requiresLogin,
        bool isBlocked,
        string? blockReason,
        bool canOrder,
        bool orderCreated,
        TableAccessOrderSummaryDto? order,
        string message)
    {
        return new TableAccessScanResponseDto
        {
            TableId = table.Id,
            TableNumber = table.TableNumber,
            BookingId = bookingId,
            HasBooking = hasBooking,
            IsBookingOwner = isBookingOwner,
            IsCheckedIn = isCheckedIn,
            CheckInPerformed = checkInPerformed,
            RequiresLogin = requiresLogin,
            IsBlocked = isBlocked,
            BlockReason = blockReason,
            CanOrder = canOrder,
            OrderCreated = orderCreated,
            Order = order,
            Message = message
        };
    }

    private static BookingFlowServiceException BuildValidationError(
        string message,
        int statusCode,
        string key,
        string error)
    {
        return new BookingFlowServiceException(
            message,
            statusCode,
            new Dictionary<string, string[]>
            {
                [key] = [error]
            });
    }

    private sealed record TableLookup(Guid Id, Guid RestaurantId, int TableNumber, bool IsActive);

    private sealed record SessionLookup(Guid Id, Guid? UserId, Guid? BookingId);

    private sealed record BookingLookup(Guid Id, Guid UserId, DateTime ReservationTimeUtc);
}
