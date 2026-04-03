using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.Bookings;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Application.Utilities;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class BookingService : IBookingService
{
    private const decimal MinimumBookingTotalAmount = 5000m;
    private const int BookingExpiryMinutes = 30;

    private readonly AppDbContext _dbContext;

    public BookingService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RestaurantTableAvailabilityDto>> GetTableAvailabilityAsync(
        Guid restaurantId,
        DateTime reservationTimeUtc,
        CancellationToken cancellationToken)
    {
        ValidateRestaurantId(restaurantId);
        ValidateReservationTimeUtc(reservationTimeUtc);

        await ExpireOverdueBookingsAsync(cancellationToken);

        var restaurant = await GetApprovedRestaurantAsync(restaurantId, cancellationToken);
        var tables = await _dbContext.RestaurantTables
            .AsNoTracking()
            .Where(table => table.RestaurantId == restaurant.Id)
            .OrderBy(table => table.TableNumber)
            .ToListAsync(cancellationToken);

        var blockedTableIds = await GetBlockedTableIdsForRequestedReservationAsync(restaurantId, reservationTimeUtc, cancellationToken);
        var activeSessionTableIds = await GetActiveSessionTableIdsAsync(restaurantId, cancellationToken);

        return tables
            .Select(table =>
            {
                var hasActiveSession = activeSessionTableIds.Contains(table.Id);
                var hasBlockingReservation = blockedTableIds.Contains(table.Id);

                return new RestaurantTableAvailabilityDto
                {
                    TableId = table.Id,
                    TableNumber = table.TableNumber,
                    IsAvailable = table.IsActive && !hasActiveSession && !hasBlockingReservation,
                    Status = !table.IsActive
                        ? "OutOfService"
                        : hasActiveSession
                            ? "Occupied"
                            : hasBlockingReservation ? "Reserved" : "Available",
                    ReservationTimeUtc = reservationTimeUtc
                };
            })
            .ToList();
    }

    public async Task<BookingDto> CreateBookingAsync(
        Guid userId,
        Guid restaurantId,
        CreateBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRestaurantId(restaurantId);
        var reservationTimeUtc = ParseReservationTimeUtc(request.ReservationTime);
        ValidateReservationTimeUtc(reservationTimeUtc, "reservationTime");

        await ExpireOverdueBookingsAsync(cancellationToken);
        await EnsureActiveUserAsync(userId, cancellationToken);

        var restaurant = await GetApprovedRestaurantAsync(restaurantId, cancellationToken);
        var table = await GetRestaurantTableAsync(restaurant.Id, request.TableId, cancellationToken);

        if (!table.IsActive)
        {
            throw BuildValidationError(
                "The selected table is not currently available for bookings.",
                StatusCodes.Status400BadRequest,
                "tableId",
                "The selected table is out of service.");
        }

        if (await HasActiveSessionAsync(table.Id, cancellationToken))
        {
            throw BuildValidationError(
                "The selected table is currently occupied by an active session.",
                StatusCodes.Status409Conflict,
                "tableId",
                "This table remains unavailable until the active session is checked out.");
        }

        if (await HasConflictingConfirmedBookingAsync(table.Id, reservationTimeUtc, cancellationToken))
        {
            throw BuildValidationError(
                "The selected table is not available at the chosen reservation time.",
                StatusCodes.Status409Conflict,
                "tableId",
                "Choose a different table or reservation time.");
        }

        if (request.Items.Count == 0)
        {
            throw BuildValidationError(
                "Booking items are required.",
                StatusCodes.Status400BadRequest,
                "items",
                "Select at least one item before creating a booking.");
        }

        var requestedMenuItemIds = request.Items
            .Select(item => item.MenuItemId)
            .Distinct()
            .ToList();

        var menuItems = await _dbContext.MenuItems
            .AsNoTracking()
            .Include(item => item.MenuCategory)
            .Where(item => requestedMenuItemIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        var menuItemsById = menuItems.ToDictionary(item => item.Id);
        var bookingItems = new List<BookingItem>();

        foreach (var requestItem in request.Items)
        {
            if (requestItem.Quantity <= 0)
            {
                throw BuildValidationError(
                    "Booking contains invalid quantities.",
                    StatusCodes.Status400BadRequest,
                    "items",
                    "Each selected item must have a quantity greater than zero.");
            }

            if (!menuItemsById.TryGetValue(requestItem.MenuItemId, out var menuItem))
            {
                throw BuildValidationError(
                    "Booking contains an invalid menu item.",
                    StatusCodes.Status400BadRequest,
                    "items",
                    "One or more selected menu items were not found.");
            }

            if (menuItem.RestaurantId != restaurantId ||
                menuItem.MenuCategory is null ||
                menuItem.MenuCategory.RestaurantId != restaurantId)
            {
                throw BuildValidationError(
                    "Booking contains menu items from a different restaurant.",
                    StatusCodes.Status400BadRequest,
                    "items",
                    "All selected items must belong to the selected restaurant.");
            }

            if (!menuItem.IsAvailable || !menuItem.MenuCategory.IsActive)
            {
                throw BuildValidationError(
                    "Booking contains unavailable menu items.",
                    StatusCodes.Status400BadRequest,
                    "items",
                    "Remove unavailable items before creating the booking.");
            }

            bookingItems.Add(new BookingItem
            {
                Id = Guid.NewGuid(),
                MenuItemId = menuItem.Id,
                Quantity = requestItem.Quantity,
                UnitPrice = menuItem.Price,
                LineTotal = menuItem.Price * requestItem.Quantity
            });
        }

        var totalAmount = bookingItems.Sum(item => item.LineTotal);
        if (totalAmount < MinimumBookingTotalAmount)
        {
            throw BuildValidationError(
                "Booking total does not meet the minimum required amount.",
                StatusCodes.Status400BadRequest,
                "items",
                $"Selected food must total at least {MinimumBookingTotalAmount:0.##}.");
        }

        var nowUtc = DateTime.UtcNow;
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RestaurantId = restaurant.Id,
            RestaurantTableId = table.Id,
            ReservationTimeUtc = reservationTimeUtc,
            Status = BookingStatus.Confirmed,
            TotalAmount = totalAmount,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            Items = bookingItems
        };

        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetMyBookingAsync(userId, booking.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<BookingDto>> GetMyBookingsAsync(Guid userId, CancellationToken cancellationToken)
    {
        await ExpireOverdueBookingsAsync(cancellationToken);

        var bookings = await LoadBookingQuery()
            .Where(booking => booking.UserId == userId)
            .OrderByDescending(booking => booking.ReservationTimeUtc)
            .ToListAsync(cancellationToken);

        return bookings.Select(MapBooking).ToList();
    }

    public async Task<BookingDto> GetMyBookingAsync(Guid userId, Guid bookingId, CancellationToken cancellationToken)
    {
        await ExpireOverdueBookingsAsync(cancellationToken);

        var booking = await LoadBookingQuery()
            .FirstOrDefaultAsync(booking => booking.Id == bookingId && booking.UserId == userId, cancellationToken);

        if (booking is null)
        {
            throw new BookingFlowServiceException(
                "Booking was not found for this user.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["bookingId"] = ["The selected booking was not found for this user."]
                });
        }

        return MapBooking(booking);
    }

    public async Task<BookingDto> CancelBookingAsync(Guid userId, Guid bookingId, CancellationToken cancellationToken)
    {
        await ExpireOverdueBookingsAsync(cancellationToken);

        var booking = await _dbContext.Bookings
            .FirstOrDefaultAsync(entity => entity.Id == bookingId && entity.UserId == userId, cancellationToken);

        if (booking is null)
        {
            throw new BookingFlowServiceException(
                "Booking was not found for this user.",
                StatusCodes.Status404NotFound);
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new BookingFlowServiceException(
                "Only confirmed bookings can be cancelled.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["bookingId"] = ["The selected booking can no longer be cancelled."]
                });
        }

        if (booking.ReservationTimeUtc <= DateTime.UtcNow)
        {
            throw new BookingFlowServiceException(
                "Bookings can only be cancelled before the reservation time.",
                StatusCodes.Status400BadRequest);
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAtUtc = DateTime.UtcNow;
        booking.UpdatedAtUtc = booking.CancelledAtUtc.Value;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetMyBookingAsync(userId, booking.Id, cancellationToken);
    }

    public async Task<TableAccessDecisionDto> ScanTableAccessAsync(Guid? userId, Guid tableId, CancellationToken cancellationToken)
    {
        if (tableId == Guid.Empty)
        {
            throw BuildValidationError(
                "Table id is required.",
                StatusCodes.Status400BadRequest,
                "tableId",
                "Table id is required.");
        }

        await ExpireOverdueBookingsAsync(cancellationToken);

        var table = await _dbContext.RestaurantTables
            .AsNoTracking()
            .Where(entity => entity.Id == tableId)
            .Select(entity => new
            {
                entity.Id,
                entity.RestaurantId,
                entity.TableNumber,
                entity.IsActive,
                ApprovalStatus = entity.Restaurant != null ? entity.Restaurant.ApprovalStatus : (RestaurantApprovalStatus?)null
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

        if (!table.IsActive)
        {
            return BuildAccessDecision("Blocked", table.Id, table.TableNumber, null, null, false, "This table is currently out of service.");
        }

        var activeSession = await _dbContext.TableSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                session => session.RestaurantTableId == table.Id && session.Status == TableSessionStatus.Active,
                cancellationToken);

        if (activeSession is not null)
        {
            if (userId.HasValue && activeSession.UserId == userId.Value)
            {
                return BuildAccessDecision("MenuAllowed", table.Id, table.TableNumber, activeSession.BookingId, activeSession.Id, true, "You already have an active table session.");
            }

            return BuildAccessDecision("Blocked", table.Id, table.TableNumber, activeSession.BookingId, activeSession.Id, true, "This table is currently occupied.");
        }

        var nowUtc = DateTime.UtcNow;
        var activeBooking = await _dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.RestaurantTableId == table.Id &&
                booking.Status == BookingStatus.Confirmed &&
                booking.ReservationTimeUtc >= nowUtc.AddMinutes(-BookingExpiryMinutes) &&
                booking.ReservationTimeUtc <= nowUtc)
            .OrderBy(booking => booking.ReservationTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeBooking is null)
        {
            return BuildAccessDecision("MenuAllowed", table.Id, table.TableNumber, null, null, false, "This table is available for menu access.");
        }

        if (!userId.HasValue)
        {
            return BuildAccessDecision("LoginRequired", table.Id, table.TableNumber, activeBooking.Id, null, false, "Log in to continue with this reserved table.");
        }

        if (activeBooking.UserId != userId.Value)
        {
            return BuildAccessDecision("Blocked", table.Id, table.TableNumber, activeBooking.Id, null, false, "This table is reserved for another booking.");
        }

        var checkInOpensAtUtc = activeBooking.ReservationTimeUtc;
        if (nowUtc < checkInOpensAtUtc)
        {
            return BuildAccessDecision("Blocked", table.Id, table.TableNumber, activeBooking.Id, null, false, "Your booking check-in window has not opened yet.");
        }

        return BuildAccessDecision("CheckInRequired", table.Id, table.TableNumber, activeBooking.Id, null, false, "Check in to start your table session.");
    }

    public async Task<BookingCheckInResponseDto> CheckInAsync(Guid userId, Guid bookingId, CancellationToken cancellationToken)
    {
        await ExpireOverdueBookingsAsync(cancellationToken);

        var booking = await _dbContext.Bookings
            .Include(entity => entity.RestaurantTable)
            .FirstOrDefaultAsync(entity => entity.Id == bookingId && entity.UserId == userId, cancellationToken);

        if (booking is null)
        {
            throw new BookingFlowServiceException(
                "Booking was not found for this user.",
                StatusCodes.Status404NotFound);
        }

        if (booking.Status != BookingStatus.Confirmed && booking.Status != BookingStatus.CheckedIn)
        {
            throw new BookingFlowServiceException(
                "Only valid confirmed bookings can be checked in.",
                StatusCodes.Status400BadRequest);
        }

        var nowUtc = DateTime.UtcNow;
        if (nowUtc < booking.ReservationTimeUtc)
        {
            throw new BookingFlowServiceException(
                "Check-in is not yet available for this booking.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["bookingId"] = ["Check-in opens at the reservation time."]
                });
        }

        if (nowUtc > GetReservationWindowEndsAtUtc(booking.ReservationTimeUtc))
        {
            if (booking.Status == BookingStatus.Confirmed)
            {
                MarkBookingAsNoShow(booking, nowUtc);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            throw new BookingFlowServiceException(
                "This booking is already marked as no-show because the check-in window has ended.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["bookingId"] = ["Check-in must happen within 30 minutes after the reservation time."]
                });
        }

        var session = await _dbContext.TableSessions
            .FirstOrDefaultAsync(
                entity => entity.BookingId == booking.Id && entity.Status == TableSessionStatus.Active,
                cancellationToken);

        if (session is null)
        {
            var blockingSession = await _dbContext.TableSessions
                .FirstOrDefaultAsync(
                    entity => entity.RestaurantTableId == booking.RestaurantTableId &&
                        entity.Status == TableSessionStatus.Active &&
                        entity.BookingId != booking.Id,
                    cancellationToken);

            if (blockingSession is not null)
            {
                throw new BookingFlowServiceException(
                    "This table is already occupied by another active session.",
                    StatusCodes.Status409Conflict,
                    new Dictionary<string, string[]>
                    {
                        ["bookingId"] = ["This table cannot be checked in until the active session is completed."]
                    });
            }

            session = new TableSession
            {
                Id = Guid.NewGuid(),
                RestaurantId = booking.RestaurantId,
                RestaurantTableId = booking.RestaurantTableId,
                BookingId = booking.Id,
                UserId = booking.UserId,
                Status = TableSessionStatus.Active,
                OpenedAtUtc = nowUtc
            };

            _dbContext.TableSessions.Add(session);
        }

        booking.Status = BookingStatus.CheckedIn;
        booking.CheckedInAtUtc ??= nowUtc;
        booking.UpdatedAtUtc = nowUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new BookingCheckInResponseDto
        {
            BookingId = booking.Id,
            SessionId = session.Id,
            RestaurantId = booking.RestaurantId,
            RestaurantTableId = booking.RestaurantTableId,
            TableNumber = booking.RestaurantTable?.TableNumber ?? 0,
            Status = booking.Status.ToString(),
            CheckedInAtUtc = booking.CheckedInAtUtc ?? nowUtc
        };
    }

    public async Task<OwnerCheckoutTableSessionResponseDto> CheckoutTableSessionAsync(
        Guid ownerId,
        Guid sessionId,
        OwnerCheckoutTableSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (sessionId == Guid.Empty)
        {
            throw BuildValidationError(
                "Session id is required.",
                StatusCodes.Status400BadRequest,
                "sessionId",
                "Session id is required.");
        }

        var session = await _dbContext.TableSessions
            .Include(entity => entity.Restaurant)
            .Include(entity => entity.RestaurantTable)
            .Include(entity => entity.Booking)
            .FirstOrDefaultAsync(
                entity => entity.Id == sessionId &&
                    entity.Restaurant != null &&
                    entity.Restaurant.OwnerId == ownerId,
                cancellationToken);

        if (session is null)
        {
            throw new BookingFlowServiceException(
                "Table session was not found for this owner.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["sessionId"] = ["The selected table session was not found for this owner."]
                });
        }

        if (session.Status != TableSessionStatus.Active)
        {
            throw new BookingFlowServiceException(
                "Only active table sessions can be checked out.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["sessionId"] = ["The selected table session is already closed."]
                });
        }

        var nowUtc = DateTime.UtcNow;
        session.Status = TableSessionStatus.Completed;
        session.ClosedAtUtc = nowUtc;
        session.ClosedByUserAccountId = ownerId;
        session.CloseReason = NormalizeCloseReason(request.CloseReason);

        if (session.Booking is not null)
        {
            session.Booking.Status = BookingStatus.Completed;
            session.Booking.CompletedAtUtc ??= nowUtc;
            session.Booking.UpdatedAtUtc = nowUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OwnerCheckoutTableSessionResponseDto
        {
            SessionId = session.Id,
            BookingId = session.BookingId,
            RestaurantId = session.RestaurantId,
            RestaurantTableId = session.RestaurantTableId,
            TableNumber = session.RestaurantTable?.TableNumber ?? 0,
            SessionStatus = session.Status.ToString(),
            BookingStatus = session.Booking?.Status.ToString(),
            EndedAtUtc = session.ClosedAtUtc ?? nowUtc,
            CloseReason = session.CloseReason
        };
    }

    public async Task<IReadOnlyList<PublicRestaurantBookingDto>> GetPublicRestaurantBookingsAsync(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        await ExpireOverdueBookingsAsync(cancellationToken);
        await EnsurePublicApprovedRestaurantExistsAsync(restaurantId, cancellationToken);

        var bookings = await _dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.RestaurantId == restaurantId &&
                (booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.CheckedIn))
            .Include(booking => booking.RestaurantTable)
            .OrderBy(booking => booking.ReservationTimeUtc)
            .ToListAsync(cancellationToken);

        return bookings
            .Select(booking => new PublicRestaurantBookingDto
            {
                BookingId = booking.Id,
                TableId = booking.RestaurantTableId,
                TableNumber = booking.RestaurantTable?.TableNumber ?? 0,
                ReservationTime = BaghdadReservationTimeHelper.ToBaghdadLocalDisplayString(booking.ReservationTimeUtc),
                Status = MapPublicBookingStatus(booking.Status)
            })
            .ToList();
    }

    public async Task<IReadOnlyList<PublicRestaurantTableLiveStatusDto>> GetPublicRestaurantLiveTableStatusAsync(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        await ExpireOverdueBookingsAsync(cancellationToken);
        await EnsurePublicApprovedRestaurantExistsAsync(restaurantId, cancellationToken);

        var liveState = await LoadRestaurantLiveStateAsync(restaurantId, cancellationToken);

        return liveState.Tables.Select(table =>
        {
            var booking = liveState.CurrentConfirmedBookings
                .Where(entity => entity.RestaurantTableId == table.Id)
                .OrderBy(entity => entity.ReservationTimeUtc)
                .FirstOrDefault();

            var hasOpenSession = liveState.ActiveSessions.Any(session => session.RestaurantTableId == table.Id);
            var status = ResolveTableStatus(table.IsActive, hasOpenSession, booking is not null);

            return new PublicRestaurantTableLiveStatusDto
            {
                TableId = table.Id,
                TableNumber = table.TableNumber,
                Status = status,
                IsAvailableForNewBooking = table.IsActive && booking is null && !hasOpenSession
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<OwnerRestaurantBookingDto>> GetOwnerBookingsAsync(
        Guid ownerId,
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        await ExpireOverdueBookingsAsync(cancellationToken);
        await EnsureApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);

        var bookings = await _dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.RestaurantId == restaurantId)
            .Include(booking => booking.User)
            .Include(booking => booking.RestaurantTable)
            .OrderByDescending(booking => booking.ReservationTimeUtc)
            .ToListAsync(cancellationToken);

        return bookings
            .Select(booking => new OwnerRestaurantBookingDto
            {
                BookingId = booking.Id,
                UserId = booking.UserId,
                UserFullName = booking.User != null ? booking.User.FullName : string.Empty,
                UserPhoneNumber = booking.User != null ? booking.User.PhoneNumber : string.Empty,
                RestaurantTableId = booking.RestaurantTableId,
                TableNumber = booking.RestaurantTable != null ? booking.RestaurantTable.TableNumber : 0,
                ReservationTimeUtc = booking.ReservationTimeUtc,
                Status = booking.Status.ToString(),
                TotalAmount = booking.TotalAmount
            })
            .ToList();
    }

    public async Task<IReadOnlyList<OwnerRestaurantTableLiveStatusDto>> GetOwnerLiveTableStatusAsync(
        Guid ownerId,
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        await ExpireOverdueBookingsAsync(cancellationToken);
        await EnsureApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);

        var liveState = await LoadRestaurantLiveStateAsync(restaurantId, cancellationToken);

        return liveState.Tables.Select(table =>
        {
            var activeSession = liveState.ActiveSessions
                .FirstOrDefault(session => session.RestaurantTableId == table.Id);

            var booking = liveState.CurrentConfirmedBookings
                .Where(entity => entity.RestaurantTableId == table.Id)
                .OrderBy(entity => entity.ReservationTimeUtc)
                .FirstOrDefault();

            var currentBooking = activeSession?.Booking ?? booking;
            var status = ResolveTableStatus(table.IsActive, activeSession is not null, booking is not null);

            return new OwnerRestaurantTableLiveStatusDto
            {
                TableId = table.Id,
                TableNumber = table.TableNumber,
                Status = status,
                CurrentBookingId = currentBooking?.Id,
                CurrentBookingStatus = currentBooking?.Status.ToString(),
                ReservationTimeUtc = currentBooking?.ReservationTimeUtc
            };
        }).ToList();
    }

    internal async Task<int> ExpireOverdueBookingsAsync(CancellationToken cancellationToken)
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
            MarkBookingAsNoShow(booking, nowUtc);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return overdueBookings.Count;
    }

    private IQueryable<Booking> LoadBookingQuery()
    {
        return _dbContext.Bookings
            .AsNoTracking()
            .Include(booking => booking.RestaurantTable)
            .Include(booking => booking.Items)
                .ThenInclude(item => item.MenuItem)
            .Include(booking => booking.TableSessions);
    }

    private async Task EnsureActiveUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userExists = await _dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId && user.IsActive, cancellationToken);

        if (!userExists)
        {
            throw new BookingFlowServiceException(
                "Authenticated user account was not found.",
                StatusCodes.Status401Unauthorized);
        }
    }

    private async Task<Restaurant> GetApprovedRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken)
    {
        var restaurant = await _dbContext.Restaurants
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == restaurantId, cancellationToken);

        if (restaurant is null)
        {
            throw new BookingFlowServiceException(
                "Restaurant was not found.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["The selected restaurant was not found."]
                });
        }

        if (restaurant.ApprovalStatus != RestaurantApprovalStatus.Approved)
        {
            throw new BookingFlowServiceException(
                "Bookings are available only for approved restaurants.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["The selected restaurant is not available for bookings."]
                });
        }

        return restaurant;
    }

    private async Task<RestaurantTable> GetRestaurantTableAsync(Guid restaurantId, Guid tableId, CancellationToken cancellationToken)
    {
        if (tableId == Guid.Empty)
        {
            throw BuildValidationError("Table id is required.", StatusCodes.Status400BadRequest, "tableId", "Table id is required.");
        }

        var table = await _dbContext.RestaurantTables
            .FirstOrDefaultAsync(entity => entity.Id == tableId && entity.RestaurantId == restaurantId, cancellationToken);

        if (table is null)
        {
            throw new BookingFlowServiceException(
                "Restaurant table was not found for this restaurant.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["tableId"] = ["The selected table was not found for this restaurant."]
                });
        }

        return table;
    }

    private async Task EnsureApprovedOwnerRestaurantAsync(Guid ownerId, Guid restaurantId, CancellationToken cancellationToken)
    {
        var restaurantExists = await _dbContext.Restaurants
            .AsNoTracking()
            .AnyAsync(
                restaurant => restaurant.Id == restaurantId &&
                    restaurant.OwnerId == ownerId &&
                    restaurant.ApprovalStatus == RestaurantApprovalStatus.Approved,
                cancellationToken);

        if (!restaurantExists)
        {
            throw new BookingFlowServiceException(
                "Restaurant was not found for this owner.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["The selected restaurant was not found for this owner."]
                });
        }
    }

    private async Task EnsurePublicApprovedRestaurantExistsAsync(Guid restaurantId, CancellationToken cancellationToken)
    {
        ValidateRestaurantId(restaurantId);

        var restaurantExists = await _dbContext.Restaurants
            .AsNoTracking()
            .AnyAsync(
                restaurant => restaurant.Id == restaurantId &&
                    restaurant.ApprovalStatus == RestaurantApprovalStatus.Approved,
                cancellationToken);

        if (!restaurantExists)
        {
            throw new BookingFlowServiceException(
                "Restaurant was not found or is not publicly available.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["restaurantId"] = ["The selected restaurant was not found or is not approved."]
                });
        }
    }

    private async Task<RestaurantLiveStateData> LoadRestaurantLiveStateAsync(Guid restaurantId, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var reservationWindowLowerBoundUtc = nowUtc.AddMinutes(-BookingExpiryMinutes);

        var tables = await _dbContext.RestaurantTables
            .AsNoTracking()
            .Where(table => table.RestaurantId == restaurantId)
            .OrderBy(table => table.TableNumber)
            .ToListAsync(cancellationToken);

        var activeBookings = await _dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.RestaurantId == restaurantId &&
                booking.Status == BookingStatus.Confirmed &&
                booking.ReservationTimeUtc >= reservationWindowLowerBoundUtc &&
                booking.ReservationTimeUtc <= nowUtc)
            .ToListAsync(cancellationToken);

        var openSessions = await _dbContext.TableSessions
            .AsNoTracking()
            .Include(session => session.Booking)
            .Where(session => session.RestaurantId == restaurantId && session.Status == TableSessionStatus.Active)
            .ToListAsync(cancellationToken);

        return new RestaurantLiveStateData(tables, activeBookings, openSessions);
    }

    private async Task<HashSet<Guid>> GetBlockedTableIdsForRequestedReservationAsync(
        Guid restaurantId,
        DateTime reservationTimeUtc,
        CancellationToken cancellationToken)
    {
        var lowerBoundUtc = reservationTimeUtc.AddMinutes(-BookingExpiryMinutes);
        var upperBoundUtc = reservationTimeUtc.AddMinutes(BookingExpiryMinutes);

        var blockedTableIds = await _dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.RestaurantId == restaurantId &&
                booking.Status == BookingStatus.Confirmed &&
                booking.ReservationTimeUtc > lowerBoundUtc &&
                booking.ReservationTimeUtc < upperBoundUtc)
            .Select(booking => booking.RestaurantTableId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return blockedTableIds.ToHashSet();
    }

    private async Task<HashSet<Guid>> GetActiveSessionTableIdsAsync(Guid restaurantId, CancellationToken cancellationToken)
    {
        var activeSessionTableIds = await _dbContext.TableSessions
            .AsNoTracking()
            .Where(session => session.RestaurantId == restaurantId && session.Status == TableSessionStatus.Active)
            .Select(session => session.RestaurantTableId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return activeSessionTableIds.ToHashSet();
    }

    private async Task<bool> HasConflictingConfirmedBookingAsync(
        Guid tableId,
        DateTime reservationTimeUtc,
        CancellationToken cancellationToken)
    {
        var lowerBoundUtc = reservationTimeUtc.AddMinutes(-BookingExpiryMinutes);
        var upperBoundUtc = reservationTimeUtc.AddMinutes(BookingExpiryMinutes);

        return await _dbContext.Bookings.AnyAsync(
            booking => booking.RestaurantTableId == tableId &&
                booking.Status == BookingStatus.Confirmed &&
                booking.ReservationTimeUtc > lowerBoundUtc &&
                booking.ReservationTimeUtc < upperBoundUtc,
            cancellationToken);
    }

    private async Task<bool> HasActiveSessionAsync(Guid tableId, CancellationToken cancellationToken)
    {
        return await _dbContext.TableSessions.AnyAsync(
            session => session.RestaurantTableId == tableId && session.Status == TableSessionStatus.Active,
            cancellationToken);
    }

    private static void ValidateRestaurantId(Guid restaurantId)
    {
        if (restaurantId == Guid.Empty)
        {
            throw BuildValidationError("Restaurant id is required.", StatusCodes.Status400BadRequest, "restaurantId", "Restaurant id is required.");
        }
    }

    private static DateTime ParseReservationTimeUtc(string? reservationTime)
    {
        if (BaghdadReservationTimeHelper.TryParseReservationTimeToUtc(reservationTime, out var reservationTimeUtc))
        {
            return reservationTimeUtc;
        }

        throw BuildValidationError(
            "Reservation time format is invalid.",
            StatusCodes.Status400BadRequest,
            "reservationTime",
            $"Reservation time must match the Baghdad local format {BaghdadReservationTimeHelper.ReservationTimeFormat}.");
    }

    private static void ValidateReservationTimeUtc(DateTime reservationTimeUtc, string key = "reservationTimeUtc")
    {
        if (reservationTimeUtc == default)
        {
            throw BuildValidationError(
                "Reservation time is required.",
                StatusCodes.Status400BadRequest,
                key,
                "Reservation time is required.");
        }

        if (reservationTimeUtc <= DateTime.UtcNow)
        {
            throw BuildValidationError(
                "Reservation time must be in the future.",
                StatusCodes.Status400BadRequest,
                key,
                "Please choose a future reservation time.");
        }
    }

    private static void MarkBookingAsNoShow(Booking booking, DateTime nowUtc)
    {
        booking.Status = BookingStatus.NoShow;
        booking.NoShowMarkedAtUtc ??= nowUtc;
        booking.UpdatedAtUtc = nowUtc;
    }

    private static DateTime GetReservationWindowEndsAtUtc(DateTime reservationTimeUtc)
    {
        return reservationTimeUtc.AddMinutes(BookingExpiryMinutes);
    }

    private static string ResolveTableStatus(bool isTableActive, bool hasActiveSession, bool hasCurrentConfirmedBooking)
    {
        if (!isTableActive)
        {
            return "OutOfService";
        }

        if (hasActiveSession)
        {
            return "Occupied";
        }

        if (hasCurrentConfirmedBooking)
        {
            return "Reserved";
        }

        return "Available";
    }

    private static string? NormalizeCloseReason(string? closeReason)
    {
        return string.IsNullOrWhiteSpace(closeReason)
            ? null
            : closeReason.Trim();
    }

    private static BookingDto MapBooking(Booking booking)
    {
        return new BookingDto
        {
            BookingId = booking.Id,
            UserId = booking.UserId,
            RestaurantId = booking.RestaurantId,
            RestaurantTableId = booking.RestaurantTableId,
            TableNumber = booking.RestaurantTable?.TableNumber ?? 0,
            ReservationTimeUtc = booking.ReservationTimeUtc,
            Status = booking.Status.ToString(),
            TotalAmount = booking.TotalAmount,
            CreatedAtUtc = booking.CreatedAtUtc,
            UpdatedAtUtc = booking.UpdatedAtUtc,
            CheckedInAtUtc = booking.CheckedInAtUtc,
            CompletedAtUtc = booking.CompletedAtUtc,
            CancelledAtUtc = booking.CancelledAtUtc,
            NoShowMarkedAtUtc = booking.NoShowMarkedAtUtc,
            ExpiredAtUtc = booking.NoShowMarkedAtUtc,
            SessionId = booking.TableSessions
                .Where(session => session.Status == TableSessionStatus.Active)
                .OrderByDescending(session => session.OpenedAtUtc)
                .Select(session => (Guid?)session.Id)
                .FirstOrDefault(),
            Items = booking.Items
                .Select(item => new BookingItemDto
                {
                    MenuItemId = item.MenuItemId,
                    MenuItemName = item.MenuItem?.Name ?? string.Empty,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = item.LineTotal
                })
            .ToList()
        };
    }

    private static string MapPublicBookingStatus(BookingStatus status)
    {
        return status switch
        {
            BookingStatus.CheckedIn => "Occupied",
            BookingStatus.Confirmed => "Reserved",
            _ => status.ToString()
        };
    }

    private static TableAccessDecisionDto BuildAccessDecision(
        string accessMode,
        Guid tableId,
        int tableNumber,
        Guid? bookingId,
        Guid? sessionId,
        bool isCheckedIn,
        string message)
    {
        return new TableAccessDecisionDto
        {
            AccessMode = accessMode,
            TableId = tableId,
            TableNumber = tableNumber,
            BookingId = bookingId,
            SessionId = sessionId,
            IsCheckedIn = isCheckedIn,
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

    private sealed record RestaurantLiveStateData(
        IReadOnlyList<RestaurantTable> Tables,
        IReadOnlyList<Booking> CurrentConfirmedBookings,
        IReadOnlyList<TableSession> ActiveSessions);
}
