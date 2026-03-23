using FluentValidation;
using SmartDiningSystem.Application.DTOs.RestaurantTables;

namespace SmartDiningSystem.Application.Validation.RestaurantTables;

public class BulkCreateRestaurantTablesRequestDtoValidator : AbstractValidator<BulkCreateRestaurantTablesRequestDto>
{
    public BulkCreateRestaurantTablesRequestDtoValidator()
    {
        RuleFor(request => request.TableCount)
            .InclusiveBetween(1, 100);
    }
}
