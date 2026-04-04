using FluentValidation;
using SmartDiningSystem.Application.DTOs.Orders;
using SmartDiningSystem.Application.Utilities;

namespace SmartDiningSystem.Application.Validation.Orders;

public class UpdateOrderStatusRequestDtoValidator : AbstractValidator<UpdateOrderStatusRequestDto>
{
    public UpdateOrderStatusRequestDtoValidator()
    {
        RuleFor(request => request.Status)
            .NotEmpty()
            .Must(status => OrderStatusApiMapper.AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Status must be one of: OrderReceived, Preparing, Ready, Served.");
    }
}
