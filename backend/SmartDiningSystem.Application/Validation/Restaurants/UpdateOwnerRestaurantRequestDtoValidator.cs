using SmartDiningSystem.Application.Utilities;
using FluentValidation;
using SmartDiningSystem.Application.DTOs.Restaurants;

namespace SmartDiningSystem.Application.Validation.Restaurants;

public class UpdateOwnerRestaurantRequestDtoValidator : AbstractValidator<UpdateOwnerRestaurantRequestDto>
{
    public UpdateOwnerRestaurantRequestDtoValidator()
    {
        RuleFor(request => request.Name)
            .MaximumLength(200)
            .When(request => request.Name is not null);

        RuleFor(request => request.Description)
            .MaximumLength(1000)
            .When(request => request.Description is not null);

        RuleFor(request => request.Address)
            .MaximumLength(500)
            .When(request => request.Address is not null);

        RuleFor(request => request.ContactPhone)
            .MaximumLength(50)
            .When(request => request.ContactPhone is not null);
        RuleFor(request => request.ContactPhone)
            .Must(phoneNumber => IraqiPhoneNumberHelper.TryNormalize(phoneNumber, out _))
            .When(request => !string.IsNullOrWhiteSpace(request.ContactPhone))
            .WithMessage("Restaurant phone number must be a valid Iraqi mobile number.");

        RuleFor(request => request.ImageUrl)
            .MaximumLength(1000)
            .When(request => request.ImageUrl is not null);

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
            .When(request => request.Latitude.HasValue || request.Longitude.HasValue)
            .WithMessage("Latitude and longitude must both be provided together.");
    }
}
