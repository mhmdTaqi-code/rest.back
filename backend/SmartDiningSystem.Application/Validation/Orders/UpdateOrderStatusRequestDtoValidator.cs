using FluentValidation;
using SmartDiningSystem.Application.DTOs.Orders;

namespace SmartDiningSystem.Application.Validation.Orders;

public class UpdateOrderStatusRequestDtoValidator : AbstractValidator<UpdateOrderStatusRequestDto>
{
    private static readonly string[] AllowedStatuses =
    [
        "OrderReceived",
        "Preparing",
        "Ready",
        "Served"
    ];

    public UpdateOrderStatusRequestDtoValidator()
    {
        RuleFor(request => request.Status)
            .NotEmpty()
            .Must(status => AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Status must be one of: OrderReceived, Preparing, Ready, Served.");
    }
}
