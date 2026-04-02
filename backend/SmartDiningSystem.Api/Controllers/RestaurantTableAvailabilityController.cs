using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Bookings;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/restaurants/{restaurantId:guid}/tables/availability")]
public class RestaurantTableAvailabilityController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public RestaurantTableAvailabilityController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<RestaurantTableAvailabilityDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<RestaurantTableAvailabilityDto>>>> GetAvailability(
        Guid restaurantId,
        [FromQuery] DateTime reservationTimeUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            var tables = await _bookingService.GetTableAvailabilityAsync(restaurantId, reservationTimeUtc, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<RestaurantTableAvailabilityDto>>
            {
                Message = "Table availability loaded successfully.",
                Data = tables
            });
        }
        catch (BookingFlowServiceException exception)
        {
            return BuildErrorResponse<IReadOnlyList<RestaurantTableAvailabilityDto>>(exception);
        }
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
