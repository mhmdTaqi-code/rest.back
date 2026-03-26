using FluentValidation;
using SmartDiningSystem.Application.DTOs.Auth;
using SmartDiningSystem.Application.Utilities;

namespace SmartDiningSystem.Application.Validation.Auth;

public class RegisterOwnerRequestDtoValidator : AbstractValidator<RegisterOwnerRequestDto>
{
    public RegisterOwnerRequestDtoValidator()
    {
        RuleFor(request => request.FullName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200);

        RuleFor(request => request.PhoneNumber)
            .NotEmpty()
            .Must(phoneNumber => IraqiPhoneNumberHelper.TryNormalize(phoneNumber, out _))
            .WithMessage("Phone number must be a valid Iraqi mobile number.");

        RuleFor(request => request.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(100);

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);

        RuleFor(request => request.RestaurantName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.RestaurantDescription)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(request => request.RestaurantAddress)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(request => request.RestaurantPhoneNumber)
            .NotEmpty()
            .Must(phoneNumber => IraqiPhoneNumberHelper.TryNormalize(phoneNumber, out _))
            .WithMessage("Restaurant phone number must be a valid Iraqi mobile number.");
    }
}
