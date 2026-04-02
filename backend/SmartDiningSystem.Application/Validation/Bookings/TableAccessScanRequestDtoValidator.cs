using FluentValidation;
using SmartDiningSystem.Application.DTOs.Bookings;

namespace SmartDiningSystem.Application.Validation.Bookings;

public class TableAccessScanRequestDtoValidator : AbstractValidator<TableAccessScanRequestDto>
{
    public TableAccessScanRequestDtoValidator()
    {
        RuleFor(request => request.TableId)
            .NotEmpty()
            .WithMessage("Table id is required.");
    }
}
