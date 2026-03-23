using FluentValidation;
using SmartDiningSystem.Application.DTOs.RestaurantTables;

namespace SmartDiningSystem.Application.Validation.RestaurantTables;

public class UpdateRestaurantTableStatusRequestDtoValidator : AbstractValidator<UpdateRestaurantTableStatusRequestDto>
{
    public UpdateRestaurantTableStatusRequestDtoValidator()
    {
    }
}
