using FluentValidation;
using SmartDiningSystem.Application.DTOs.RestaurantTables;

namespace SmartDiningSystem.Application.Validation.RestaurantTables;

public class CreateRestaurantTableRequestDtoValidator : AbstractValidator<CreateRestaurantTableRequestDto>
{
    public CreateRestaurantTableRequestDtoValidator()
    {
        RuleFor(request => request.TableNumber)
            .GreaterThan(0);
    }
}
