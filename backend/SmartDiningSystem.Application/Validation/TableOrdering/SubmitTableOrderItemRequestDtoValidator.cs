using FluentValidation;
using SmartDiningSystem.Application.DTOs.TableOrdering;

namespace SmartDiningSystem.Application.Validation.TableOrdering;

public class SubmitTableOrderItemRequestDtoValidator : AbstractValidator<SubmitTableOrderItemRequestDto>
{
    public SubmitTableOrderItemRequestDtoValidator()
    {
        RuleFor(item => item.MenuItemId)
            .NotEmpty();

        RuleFor(item => item.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(99);
    }
}
