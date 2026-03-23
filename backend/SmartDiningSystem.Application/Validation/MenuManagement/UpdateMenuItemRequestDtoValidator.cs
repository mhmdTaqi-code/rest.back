using FluentValidation;
using SmartDiningSystem.Application.DTOs.MenuManagement;

namespace SmartDiningSystem.Application.Validation.MenuManagement;

public class UpdateMenuItemRequestDtoValidator : AbstractValidator<UpdateMenuItemRequestDto>
{
    public UpdateMenuItemRequestDtoValidator()
    {
        RuleFor(request => request.MenuCategoryId)
            .NotEmpty();

        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Description)
            .MaximumLength(1000);

        RuleFor(request => request.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(request => request.ImageUrl)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(request => request.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .When(request => request.DisplayOrder.HasValue);
    }
}
