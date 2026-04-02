using FluentValidation;
using SmartDiningSystem.Application.DTOs.Bookings;

namespace SmartDiningSystem.Application.Validation.Bookings;

public class CreateBookingItemRequestDtoValidator : AbstractValidator<CreateBookingItemRequestDto>
{
    public CreateBookingItemRequestDtoValidator()
    {
        RuleFor(item => item.MenuItemId)
            .NotEmpty()
            .WithMessage("Menu item id is required.");

        RuleFor(item => item.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
    }
}
