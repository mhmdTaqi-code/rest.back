using SmartDiningSystem.Application.DTOs.Bookings;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IBookingService
{
    Task<IReadOnlyList<RestaurantTableAvailabilityDto>> GetTableAvailabilityAsync(Guid restaurantId, DateTime reservationTimeUtc, CancellationToken cancellationToken);
    Task<BookingDto> CreateBookingAsync(Guid userId, Guid restaurantId, CreateBookingRequestDto request, CancellationToken cancellationToken);
    Task<IReadOnlyList<BookingDto>> GetMyBookingsAsync(Guid userId, CancellationToken cancellationToken);
    Task<BookingDto> GetMyBookingAsync(Guid userId, Guid bookingId, CancellationToken cancellationToken);
    Task<BookingDto> CancelBookingAsync(Guid userId, Guid bookingId, CancellationToken cancellationToken);
    Task<BookingCheckInResponseDto> CheckInAsync(Guid userId, Guid bookingId, CancellationToken cancellationToken);
    Task<OwnerCheckoutTableSessionResponseDto> CheckoutTableSessionAsync(Guid ownerId, Guid sessionId, OwnerCheckoutTableSessionRequestDto request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicRestaurantBookingDto>> GetPublicRestaurantBookingsAsync(Guid restaurantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicRestaurantTableLiveStatusDto>> GetPublicRestaurantLiveTableStatusAsync(Guid restaurantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OwnerRestaurantBookingDto>> GetOwnerBookingsAsync(Guid ownerId, Guid restaurantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OwnerRestaurantTableLiveStatusDto>> GetOwnerLiveTableStatusAsync(Guid ownerId, Guid restaurantId, CancellationToken cancellationToken);
}
