using FluentValidation;
using SmartDiningSystem.Application.DTOs.RestaurantTables;

namespace SmartDiningSystem.Application.Validation.RestaurantTables;

public class UpdateRestaurantTableStatusRequestDtoValidator : AbstractValidator<UpdateRestaurantTableStatusRequestDto>
{
    public UpdateRestaurantTableStatusRequestDtoValidator()
    {
        RuleFor(request => request.ImageUrl)
            .MaximumLength(1000)
            .Must(BeAValidAbsoluteUrl)
            .When(request => !string.IsNullOrWhiteSpace(request.ImageUrl))
            .WithMessage("Image URL must be a valid absolute URL.");
    }

    private static bool BeAValidAbsoluteUrl(string? imageUrl)
    {
        return Uri.TryCreate(imageUrl, UriKind.Absolute, out _);
    }
}
