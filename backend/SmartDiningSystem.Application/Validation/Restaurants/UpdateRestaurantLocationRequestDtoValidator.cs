using FluentValidation;
using SmartDiningSystem.Application.DTOs.Restaurants;

namespace SmartDiningSystem.Application.Validation.Restaurants;

public class UpdateRestaurantLocationRequestDtoValidator : AbstractValidator<UpdateRestaurantLocationRequestDto>
{
    public UpdateRestaurantLocationRequestDtoValidator()
    {
        RuleFor(request => request.Latitude)
            .InclusiveBetween(-90, 90)
            .When(request => request.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(request => request.Longitude)
            .InclusiveBetween(-180, 180)
            .When(request => request.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180.");

        RuleFor(request => request)
            .Must(request =>
                (request.Latitude.HasValue && request.Longitude.HasValue) ||
                (!request.Latitude.HasValue && !request.Longitude.HasValue))
            .WithMessage("Latitude and longitude must both be provided or both be null.");
    }
}
