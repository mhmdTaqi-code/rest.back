using FluentValidation;
using SmartDiningSystem.Application.DTOs.TableAccess;
using SmartDiningSystem.Application.Validation.TableOrdering;

namespace SmartDiningSystem.Application.Validation.TableAccess;

public class TableAccessScanRequestDtoValidator : AbstractValidator<TableAccessScanRequestDto>
{
    public TableAccessScanRequestDtoValidator()
    {
        RuleFor(request => request.TableId)
            .NotEmpty()
            .WithMessage("Table id is required.");

        When(request => request.Items is { Count: > 0 }, () =>
        {
            RuleForEach(request => request.Items!)
                .SetValidator(new SubmitTableOrderItemRequestDtoValidator());

            RuleFor(request => request.Items!)
                .Must(items => items.Select(item => item.MenuItemId).Distinct().Count() == items.Count)
                .WithMessage("Each menu item may only appear once in the order.");
        });
    }
}
