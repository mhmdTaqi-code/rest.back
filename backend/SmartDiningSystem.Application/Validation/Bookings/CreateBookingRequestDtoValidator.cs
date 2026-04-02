using FluentValidation;
using SmartDiningSystem.Application.DTOs.Bookings;
using SmartDiningSystem.Application.Utilities;

namespace SmartDiningSystem.Application.Validation.Bookings;

public class CreateBookingRequestDtoValidator : AbstractValidator<CreateBookingRequestDto>
{
    public CreateBookingRequestDtoValidator()
    {
        RuleFor(request => request.TableId)
            .NotEmpty()
            .WithMessage("Table id is required.");

        RuleFor(request => request.ReservationTime)
            .NotEmpty()
            .WithMessage("Reservation time is required.")
            .Must(value => BaghdadReservationTimeHelper.TryParseLocalReservationTime(value, out _))
            .WithMessage($"Reservation time must match the Baghdad local format {BaghdadReservationTimeHelper.ReservationTimeFormat}.");

        RuleFor(request => request.Items)
            .NotNull()
            .Must(items => items is { Count: > 0 })
            .WithMessage("Select at least one menu item before creating a booking.");

        RuleForEach(request => request.Items)
            .SetValidator(new CreateBookingItemRequestDtoValidator());
    }
}
