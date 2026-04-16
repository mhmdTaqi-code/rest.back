using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.TableAccess;
using SmartDiningSystem.Application.DTOs.TableAvailability;
using SmartDiningSystem.Application.DTOs.TableOrdering;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class TableAccessFlowService : ITableAccessFlowService
{
    private readonly AppDbContext _dbContext;
    private readonly BookingService _bookingService;
    private readonly ITableAvailabilityService _tableAvailabilityService;
    private readonly ITableSessionOrderService _tableSessionOrderService;

    public TableAccessFlowService(
        AppDbContext dbContext,
        BookingService bookingService,
        ITableAvailabilityService tableAvailabilityService,
        ITableSessionOrderService tableSessionOrderService)
    {
        _dbContext = dbContext;
        _bookingService = bookingService;
        _tableAvailabilityService = tableAvailabilityService;
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

        var tableState = await _tableAvailabilityService.GetTableStateAsync(request.TableId, cancellationToken);
        var table = new TableLookup(
            tableState.TableId,
            tableState.RestaurantId,
            tableState.TableNumber,
            tableState.IsTableActive,
            tableState.IsOrderingEnabled);
        var requestedItems = request.Items;
        var hasItems = requestedItems is { Count: > 0 };

        if (!tableState.IsTableActive || !tableState.IsOrderingEnabled)
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

        if (tableState.HasActiveSession)
        {
            if (!userId.HasValue || tableState.ActiveSessionUserId != userId.Value)
            {
                return CreateResponse(
                    table,
                    bookingId: null,
                    hasBooking: tableState.ActiveSessionBookingId.HasValue,
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
                    bookingId: tableState.ActiveSessionBookingId,
                    hasBooking: tableState.ActiveSessionBookingId.HasValue,
                    isBookingOwner: tableState.ActiveSessionBookingId.HasValue,
                    isCheckedIn: tableState.ActiveSessionBookingId.HasValue,
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
            var existingSessionOrder = await SubmitOrderAsync(
                userId.Value,
                tableState.ActiveSessionId!.Value,
                requestedItems!,
                cancellationToken);

            return CreateResponse(
                table,
                bookingId: tableState.ActiveSessionBookingId,
                hasBooking: tableState.ActiveSessionBookingId.HasValue,
                isBookingOwner: tableState.ActiveSessionBookingId.HasValue,
                isCheckedIn: tableState.ActiveSessionBookingId.HasValue,
                checkInPerformed: false,
                requiresLogin: false,
                isBlocked: false,
                blockReason: null,
                canOrder: true,
                orderCreated: true,
                order: MapOrder(existingSessionOrder),
                message: "Order created successfully.");
        }

        if (tableState.HasActiveBooking)
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

            if (tableState.ActiveBookingUserId != userId.Value)
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

            if (tableState.ReservationTimeUtc > DateTime.UtcNow)
            {
                return CreateResponse(
                    table,
                    bookingId: tableState.ActiveBookingId,
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

            var checkInResult = await _bookingService.CheckInAsync(
                userId.Value,
                tableState.ActiveBookingId!.Value,
                cancellationToken);

            if (!hasItems)
            {
                return CreateResponse(
                    table,
                    bookingId: tableState.ActiveBookingId,
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
                bookingId: tableState.ActiveBookingId,
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

    private static Team10CreatedOrderDto MapOrder(SubmittedTableOrderResponseDto order)
    {
        return new Team10CreatedOrderDto
        {
            OrderId = order.OrderId,
            ItemsCount = order.ItemCount,
            TotalPrice = order.TotalAmount
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
        Team10CreatedOrderDto? order,
        string message)
    {
        return new TableAccessScanResponseDto
        {
            ResultType = ResolveResultType(requiresLogin, isBlocked, blockReason, hasBooking, canOrder),
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
            OccupancyStatus = blockReason switch
            {
                "OutOfService" => "OutOfService",
                "Occupied"     => "Occupied",
                "Reserved"     => "Reserved",
                _              => "Available"
            },
            CanOrder = canOrder,
            OrderCreated = orderCreated,
            Order = order,
            Message = message
        };
    }

    private static TableAccessScanResultType ResolveResultType(
        bool requiresLogin,
        bool isBlocked,
        string? blockReason,
        bool hasBooking,
        bool canOrder)
    {
        if (requiresLogin)
        {
            return TableAccessScanResultType.Unauthorized;
        }

        if (string.Equals(blockReason, "Occupied", StringComparison.OrdinalIgnoreCase))
        {
            return TableAccessScanResultType.Occupied;
        }

        if (string.Equals(blockReason, "Reserved", StringComparison.OrdinalIgnoreCase) ||
            (hasBooking && !canOrder))
        {
            return TableAccessScanResultType.Reserved;
        }

        if (isBlocked)
        {
            return TableAccessScanResultType.Blocked;
        }

        return TableAccessScanResultType.Success;
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

    private sealed record TableLookup(
        Guid Id,
        Guid RestaurantId,
        int TableNumber,
        bool IsActive,
        bool IsOrderingEnabled);
}
