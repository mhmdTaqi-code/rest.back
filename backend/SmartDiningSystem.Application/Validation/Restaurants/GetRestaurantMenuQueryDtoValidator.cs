using FluentValidation;
using SmartDiningSystem.Application.DTOs.Restaurants;

namespace SmartDiningSystem.Application.Validation.Restaurants;

public class GetRestaurantMenuQueryDtoValidator : AbstractValidator<GetRestaurantMenuQueryDto>
{
    private static readonly string[] AllowedSortValues =
    [
        "nameAsc",
        "nameDesc",
        "priceAsc",
        "priceDesc",
        "newest",
        "oldest"
    ];

    public GetRestaurantMenuQueryDtoValidator()
    {
        RuleFor(query => query.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(query => query.MinPrice.HasValue)
            .WithMessage("Minimum price must be zero or greater.");

        RuleFor(query => query.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .When(query => query.MaxPrice.HasValue)
            .WithMessage("Maximum price must be zero or greater.");

        RuleFor(query => query)
            .Must(query => !query.MinPrice.HasValue || !query.MaxPrice.HasValue || query.MinPrice.Value <= query.MaxPrice.Value)
            .WithMessage("Minimum price must be less than or equal to maximum price.");

        RuleFor(query => query.SortBy)
            .Must(sortBy => string.IsNullOrWhiteSpace(sortBy) || AllowedSortValues.Contains(sortBy.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortBy must be one of: nameAsc, nameDesc, priceAsc, priceDesc, newest, oldest.");
    }
}
