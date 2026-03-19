using FluentValidation;
using SmartDiningSystem.Application.DTOs.Auth;
using SmartDiningSystem.Application.Utilities;

namespace SmartDiningSystem.Application.Validation.Auth;

public class VerifyOtpRequestDtoValidator : AbstractValidator<VerifyOtpRequestDto>
{
    public VerifyOtpRequestDtoValidator()
    {
        RuleFor(request => request.PhoneNumber)
            .NotEmpty()
            .Must(phoneNumber => IraqiPhoneNumberHelper.TryNormalize(phoneNumber, out _))
            .WithMessage("Phone number must be a valid Iraqi mobile number.");

        RuleFor(request => request.Code)
            .NotEmpty()
            .Length(6)
            .Matches(@"^[0-9]{6}$")
            .WithMessage("OTP code must be exactly 6 digits.");
    }
}
