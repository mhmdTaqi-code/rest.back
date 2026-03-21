using FluentValidation;
using SmartDiningSystem.Application.DTOs.TableOrdering;

namespace SmartDiningSystem.Application.Validation.TableOrdering;

public class UpdateCartItemRequestDtoValidator : AbstractValidator<UpdateCartItemRequestDto>
{
    public UpdateCartItemRequestDtoValidator()
    {
        RuleFor(request => request.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(99);
    }
}
