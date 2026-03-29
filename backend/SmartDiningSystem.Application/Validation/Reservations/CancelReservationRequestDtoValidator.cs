using FluentValidation;
using SmartDiningSystem.Application.DTOs.Reservations;

namespace SmartDiningSystem.Application.Validation.Reservations;

public class CancelReservationRequestDtoValidator : AbstractValidator<CancelReservationRequestDto>
{
    public CancelReservationRequestDtoValidator()
    {
        RuleFor(request => request.CancellationReason)
            .MaximumLength(500)
            .When(request => !string.IsNullOrWhiteSpace(request.CancellationReason));
    }
}
