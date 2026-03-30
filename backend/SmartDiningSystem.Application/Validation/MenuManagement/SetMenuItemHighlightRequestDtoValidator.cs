using FluentValidation;
using SmartDiningSystem.Application.DTOs.MenuManagement;

namespace SmartDiningSystem.Application.Validation.MenuManagement;

public class SetMenuItemHighlightRequestDtoValidator : AbstractValidator<SetMenuItemHighlightRequestDto>
{
    public SetMenuItemHighlightRequestDtoValidator()
    {
        RuleFor(request => request.HighlightTag)
            .NotEmpty()
            .MaximumLength(50)
            .Must(tag => !string.IsNullOrWhiteSpace(tag))
            .WithMessage("Highlight tag is required.");
    }
}
