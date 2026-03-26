using FluentValidation;
using SmartDiningSystem.Application.DTOs.Restaurants;

namespace SmartDiningSystem.Application.Validation.Restaurants;

public class SubmitRestaurantRatingRequestDtoValidator : AbstractValidator<SubmitRestaurantRatingRequestDto>
{
    public SubmitRestaurantRatingRequestDtoValidator()
    {
        RuleFor(request => request.Stars)
            .InclusiveBetween(1, 5)
            .WithMessage("Stars must be between 1 and 5.");

        RuleFor(request => request.Comment)
            .MaximumLength(1000)
            .When(request => !string.IsNullOrWhiteSpace(request.Comment))
            .WithMessage("Comment must not exceed 1000 characters.");
    }
}
