using FluentValidation;
using SmartDiningSystem.Application.DTOs.Auth;
using SmartDiningSystem.Application.Utilities;

namespace SmartDiningSystem.Application.Validation.Auth;

public class RegisterUserRequestDtoValidator : AbstractValidator<RegisterUserRequestDto>
{
    public RegisterUserRequestDtoValidator()
    {
        RuleFor(request => request.FullName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200);

        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

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
    }
}
