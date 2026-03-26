using FluentValidation;
using SmartDiningSystem.Application.DTOs.Restaurants;

namespace SmartDiningSystem.Application.Validation.Restaurants;

public class UpdateRestaurantImageRequestDtoValidator : AbstractValidator<UpdateRestaurantImageRequestDto>
{
    public UpdateRestaurantImageRequestDtoValidator()
    {
        RuleFor(request => request.ImageUrl)
            .MaximumLength(1000)
            .When(request => !string.IsNullOrWhiteSpace(request.ImageUrl))
            .WithMessage("Image URL must not exceed 1000 characters.");
    }
}
