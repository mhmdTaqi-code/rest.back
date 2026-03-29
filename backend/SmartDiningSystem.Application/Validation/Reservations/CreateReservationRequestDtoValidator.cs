using FluentValidation;
using SmartDiningSystem.Application.DTOs.Reservations;

namespace SmartDiningSystem.Application.Validation.Reservations;

public class CreateReservationRequestDtoValidator : AbstractValidator<CreateReservationRequestDto>
{
    public CreateReservationRequestDtoValidator()
    {
        RuleFor(request => request.RestaurantId)
            .NotEmpty();

        RuleFor(request => request.RestaurantTableId)
            .NotEmpty();

        RuleFor(request => request.ReservationStartUtc)
            .Must(start => start > DateTime.UtcNow)
            .WithMessage("Reservation start time must be in the future.");

        RuleFor(request => request.GuestCount)
            .GreaterThan(0);
    }
}
