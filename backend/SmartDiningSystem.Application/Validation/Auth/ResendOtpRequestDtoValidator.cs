using FluentValidation;
using SmartDiningSystem.Application.DTOs.Auth;
using SmartDiningSystem.Application.Utilities;

namespace SmartDiningSystem.Application.Validation.Auth;

public class ResendOtpRequestDtoValidator : AbstractValidator<ResendOtpRequestDto>
{
    public ResendOtpRequestDtoValidator()
    {
        RuleFor(request => request.PhoneNumber)
            .NotEmpty()
            .Must(phoneNumber => IraqiPhoneNumberHelper.TryNormalize(phoneNumber, out _))
            .WithMessage("Phone number must be a valid Iraqi mobile number.");
    }
}
