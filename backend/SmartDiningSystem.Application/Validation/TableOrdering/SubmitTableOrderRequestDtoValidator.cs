using FluentValidation;
using SmartDiningSystem.Application.DTOs.TableOrdering;

namespace SmartDiningSystem.Application.Validation.TableOrdering;

public class SubmitTableOrderRequestDtoValidator : AbstractValidator<SubmitTableOrderRequestDto>
{
    public SubmitTableOrderRequestDtoValidator()
    {
        RuleFor(request => request.Items)
            .NotNull()
            .Must(items => items is { Count: > 0 })
            .WithMessage("Add at least one item before submitting an order.");

        RuleForEach(request => request.Items)
            .SetValidator(new SubmitTableOrderItemRequestDtoValidator());

        RuleFor(request => request.Items)
            .Must(items => items is null || items.Select(item => item.MenuItemId).Distinct().Count() == items.Count)
            .WithMessage("Each menu item may only appear once in the order.");
    }
}
