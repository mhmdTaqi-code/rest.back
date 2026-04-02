using SmartDiningSystem.Application.Utilities;
using FluentValidation;
using SmartDiningSystem.Application.DTOs.Restaurants;

namespace SmartDiningSystem.Application.Validation.Restaurants;

public class CreateOwnerRestaurantRequestDtoValidator : AbstractValidator<CreateOwnerRestaurantRequestDto>
{
    public CreateOwnerRestaurantRequestDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Description)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(request => request.Address)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(request => request.ContactPhone)
            .NotEmpty()
            .MaximumLength(50);
        RuleFor(request => request.ContactPhone)
            .Must(phoneNumber => IraqiPhoneNumberHelper.TryNormalize(phoneNumber, out _))
            .WithMessage("Restaurant phone number must be a valid Iraqi mobile number.");

        RuleFor(request => request.ImageUrl)
            .MaximumLength(1000)
            .When(request => !string.IsNullOrWhiteSpace(request.ImageUrl));

        RuleFor(request => request.Latitude)
            .InclusiveBetween(-90, 90)
            .When(request => request.Latitude.HasValue);

        RuleFor(request => request.Longitude)
            .InclusiveBetween(-180, 180)
            .When(request => request.Longitude.HasValue);

        RuleFor(request => request)
            .Must(request =>
                (request.Latitude.HasValue && request.Longitude.HasValue) ||
                (!request.Latitude.HasValue && !request.Longitude.HasValue))
            .WithMessage("Latitude and longitude must both be provided or both be omitted.");
    }
}
