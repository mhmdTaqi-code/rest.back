using FluentValidation;
using SmartDiningSystem.Application.DTOs.Bookings;

namespace SmartDiningSystem.Application.Validation.Bookings;

public class OwnerCheckoutTableSessionRequestDtoValidator : AbstractValidator<OwnerCheckoutTableSessionRequestDto>
{
    public OwnerCheckoutTableSessionRequestDtoValidator()
    {
        RuleFor(request => request.CloseReason)
            .MaximumLength(500)
            .WithMessage("Close reason must not exceed 500 characters.");
    }
}
