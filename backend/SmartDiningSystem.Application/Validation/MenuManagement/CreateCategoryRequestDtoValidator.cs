using FluentValidation;
using SmartDiningSystem.Application.DTOs.MenuManagement;

namespace SmartDiningSystem.Application.Validation.MenuManagement;

public class CreateCategoryRequestDtoValidator : AbstractValidator<CreateCategoryRequestDto>
{
    public CreateCategoryRequestDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.Description)
            .MaximumLength(1000);

        RuleFor(request => request.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .When(request => request.DisplayOrder.HasValue);
    }
}
