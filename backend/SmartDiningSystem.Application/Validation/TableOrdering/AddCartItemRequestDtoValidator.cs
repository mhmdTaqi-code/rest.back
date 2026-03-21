using FluentValidation;
using SmartDiningSystem.Application.DTOs.TableOrdering;

namespace SmartDiningSystem.Application.Validation.TableOrdering;

public class AddCartItemRequestDtoValidator : AbstractValidator<AddCartItemRequestDto>
{
    public AddCartItemRequestDtoValidator()
    {
        RuleFor(request => request.MenuItemId)
            .NotEmpty();

        RuleFor(request => request.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(99);
    }
}
