using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Bookings;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/restaurants/{restaurantId:guid}/bookings")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class RestaurantBookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public RestaurantBookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<BookingDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiSuccessResponseDto<BookingDto>>> Create(
        Guid restaurantId,
        [FromBody] CreateBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var booking = await _bookingService.CreateBookingAsync(userId.Value, restaurantId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiSuccessResponseDto<BookingDto>
            {
                Message = "Booking created successfully.",
                Data = booking
            });
        }
        catch (BookingFlowServiceException exception)
        {
            return BuildErrorResponse<BookingDto>(exception);
        }
    }

    private Guid? GetUserId()
    {
        var userId = User.FindFirstValue("userId");
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private ApiErrorResponseDto BuildUnauthorizedResponse()
    {
        return new ApiErrorResponseDto
        {
            Message = "Unauthorized user context.",
            TraceId = HttpContext.TraceIdentifier
        };
    }

    private ActionResult<ApiSuccessResponseDto<T>> BuildErrorResponse<T>(BookingFlowServiceException exception)
    {
        return StatusCode(exception.StatusCode, new ApiErrorResponseDto
        {
            Message = exception.Message,
            Errors = exception.Errors,
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
