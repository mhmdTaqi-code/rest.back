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

        var blockingBookings = await _dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.RestaurantId == restaurantId &&
                booking.ReservationTimeUtc == reservationTimeUtc &&
                (booking.Status == BookingStatus.Reserved || booking.Status == BookingStatus.CheckedIn))
            .ToListAsync(cancellationToken);

        var blockedTableIds = blockingBookings
            .Select(booking => booking.RestaurantTableId)
            .ToHashSet();

        return tables
            .Select(table => new RestaurantTableAvailabilityDto
            {
                TableId = table.Id,
                TableNumber = table.TableNumber,
                IsAvailable = table.IsActive && !blockedTableIds.Contains(table.Id),
                Status = !table.IsActive
                    ? "OutOfService"
                    : blockedTableIds.Contains(table.Id) ? "Reserved" : "Available",
                ReservationTimeUtc = reservationTimeUtc
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

        var isTableBlocked = await _dbContext.Bookings
            .AnyAsync(
                booking => booking.RestaurantTableId == table.Id &&
                    booking.ReservationTimeUtc == reservationTimeUtc &&
                    (booking.Status == BookingStatus.Reserved || booking.Status == BookingStatus.CheckedIn),
                cancellationToken);

        if (isTableBlocked)
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
            Status = BookingStatus.Reserved,
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

        if (booking.Status != BookingStatus.Reserved)
        {
            throw new BookingFlowServiceException(
                "Only reserved bookings can be cancelled.",
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

    public async Task<TableAccessDecisionDto> ScanTableAccessAsync(Guid? userId, TableAccessScanRequestDto request, CancellationToken cancellationToken)
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

        await ExpireOverdueBookingsAsync(cancellationToken);

        var table = await _dbContext.RestaurantTables
            .AsNoTracking()
            .Where(entity => entity.Id == request.TableId)
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

        var openSession = await _dbContext.TableSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                session => session.RestaurantTableId == table.Id && session.Status == TableSessionStatus.Open,
                cancellationToken);

        if (openSession is not null)
        {
            if (userId.HasValue && openSession.UserId == userId.Value)
            {
                return BuildAccessDecision("MenuAllowed", table.Id, table.TableNumber, openSession.BookingId, openSession.Id, true, "You already have an active table session.");
            }

            return BuildAccessDecision("Blocked", table.Id, table.TableNumber, openSession.BookingId, openSession.Id, true, "This table is currently occupied.");
        }

        var nowUtc = DateTime.UtcNow;
        var activeBooking = await _dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.RestaurantTableId == table.Id &&
                (booking.Status == BookingStatus.Reserved || booking.Status == BookingStatus.CheckedIn))
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

        if (activeBooking.Status == BookingStatus.CheckedIn)
        {
            var latestSession = await _dbContext.TableSessions
                .AsNoTracking()
                .Where(session => session.BookingId == activeBooking.Id && session.Status == TableSessionStatus.Open)
                .OrderByDescending(session => session.OpenedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            return BuildAccessDecision("MenuAllowed", table.Id, table.TableNumber, activeBooking.Id, latestSession?.Id, true, "Your booking is already checked in.");
        }

        var checkInOpensAtUtc = activeBooking.ReservationTimeUtc;
        var expiresAtUtc = activeBooking.ReservationTimeUtc.AddMinutes(BookingExpiryMinutes);
        if (nowUtc < checkInOpensAtUtc)
        {
            return BuildAccessDecision("Blocked", table.Id, table.TableNumber, activeBooking.Id, null, false, "Your booking check-in window has not opened yet.");
        }

        if (nowUtc > expiresAtUtc)
        {
            return BuildAccessDecision("Blocked", table.Id, table.TableNumber, activeBooking.Id, null, false, "This booking has already expired.");
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

        if (booking.Status != BookingStatus.Reserved && booking.Status != BookingStatus.CheckedIn)
        {
            throw new BookingFlowServiceException(
                "Only valid reserved bookings can be checked in.",
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

        if (nowUtc > booking.ReservationTimeUtc.AddMinutes(BookingExpiryMinutes))
        {
            booking.Status = BookingStatus.Expired;
            booking.ExpiredAtUtc = nowUtc;
            booking.UpdatedAtUtc = nowUtc;
            await _dbContext.SaveChangesAsync(cancellationToken);

            throw new BookingFlowServiceException(
                "This booking has already expired.",
                StatusCodes.Status400BadRequest);
        }

        var session = await _dbContext.TableSessions
            .FirstOrDefaultAsync(
                entity => entity.BookingId == booking.Id && entity.Status == TableSessionStatus.Open,
                cancellationToken);

        if (session is null)
        {
            session = new TableSession
            {
                Id = Guid.NewGuid(),
                RestaurantId = booking.RestaurantId,
                RestaurantTableId = booking.RestaurantTableId,
                BookingId = booking.Id,
                UserId = booking.UserId,
                Status = TableSessionStatus.Open,
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
                (booking.Status == BookingStatus.Reserved || booking.Status == BookingStatus.CheckedIn))
            .Include(booking => booking.RestaurantTable)
            .OrderBy(booking => booking.ReservationTimeUtc)
            .ToListAsync(cancellationToken);

        return bookings
            .Select(booking => new PublicRestaurantBookingDto
            {
                BookingId = booking.Id,
                TableId = booking.RestaurantTableId,
                TableNumber = booking.RestaurantTable?.TableNumber ?? 0,
                ReservationStart = BaghdadReservationTimeHelper.ToBaghdadLocalDisplayString(booking.ReservationTimeUtc),
                ReservationEnd = BaghdadReservationTimeHelper.ToBaghdadLocalDisplayString(
                    booking.ReservationTimeUtc.AddMinutes(BookingExpiryMinutes)),
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
            var booking = liveState.ActiveBookings
                .Where(entity => entity.RestaurantTableId == table.Id)
                .OrderBy(entity => entity.ReservationTimeUtc)
                .FirstOrDefault();

            var hasOpenSession = liveState.OpenSessions.Any(session => session.RestaurantTableId == table.Id);
            var status = !table.IsActive
                ? "OutOfService"
                : hasOpenSession
                    ? "Occupied"
                    : booking is not null
                        ? "Reserved"
                        : "Available";

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
            var booking = liveState.ActiveBookings
                .Where(entity => entity.RestaurantTableId == table.Id)
                .OrderBy(entity => entity.ReservationTimeUtc)
                .FirstOrDefault();

            var hasOpenSession = liveState.OpenSessions.Any(session => session.RestaurantTableId == table.Id);
            var status = !table.IsActive
                ? "OutOfService"
                : hasOpenSession
                    ? "Occupied"
                    : booking is not null
                        ? "Reserved"
                        : "Available";

            return new OwnerRestaurantTableLiveStatusDto
            {
                TableId = table.Id,
                TableNumber = table.TableNumber,
                Status = status,
                CurrentBookingId = booking?.Id,
                CurrentBookingStatus = booking?.Status.ToString(),
                ReservationTimeUtc = booking?.ReservationTimeUtc
            };
        }).ToList();
    }

    internal async Task<int> ExpireOverdueBookingsAsync(CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var overdueBookings = await _dbContext.Bookings
            .Where(booking =>
                booking.Status == BookingStatus.Reserved &&
                booking.ReservationTimeUtc.AddMinutes(BookingExpiryMinutes) < nowUtc)
            .ToListAsync(cancellationToken);

        if (overdueBookings.Count == 0)
        {
            return 0;
        }

        foreach (var booking in overdueBookings)
        {
            booking.Status = BookingStatus.Expired;
            booking.ExpiredAtUtc = nowUtc;
            booking.UpdatedAtUtc = nowUtc;
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
        var tables = await _dbContext.RestaurantTables
            .AsNoTracking()
            .Where(table => table.RestaurantId == restaurantId)
            .OrderBy(table => table.TableNumber)
            .ToListAsync(cancellationToken);

        var activeBookings = await _dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.RestaurantId == restaurantId &&
                (booking.Status == BookingStatus.Reserved || booking.Status == BookingStatus.CheckedIn))
            .ToListAsync(cancellationToken);

        var openSessions = await _dbContext.TableSessions
            .AsNoTracking()
            .Where(session => session.RestaurantId == restaurantId && session.Status == TableSessionStatus.Open)
            .ToListAsync(cancellationToken);

        return new RestaurantLiveStateData(tables, activeBookings, openSessions);
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
            CancelledAtUtc = booking.CancelledAtUtc,
            ExpiredAtUtc = booking.ExpiredAtUtc,
            SessionId = booking.TableSessions
                .Where(session => session.Status == TableSessionStatus.Open)
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
            _ => "Reserved"
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
        IReadOnlyList<Booking> ActiveBookings,
        IReadOnlyList<TableSession> OpenSessions);
}
