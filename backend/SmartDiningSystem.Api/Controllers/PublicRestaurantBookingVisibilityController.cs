using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Bookings;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/restaurants/{restaurantId:guid}")]
public class PublicRestaurantBookingVisibilityController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public PublicRestaurantBookingVisibilityController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("bookings/public")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantBookingDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantBookingDto>>>> GetPublicBookings(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var bookings = await _bookingService.GetPublicRestaurantBookingsAsync(restaurantId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantBookingDto>>
            {
                Message = "Public restaurant bookings loaded successfully.",
                Data = bookings
            });
        }
        catch (BookingFlowServiceException exception)
        {
            return BuildErrorResponse<IReadOnlyList<PublicRestaurantBookingDto>>(exception);
        }
    }

    [HttpGet("tables/live-status")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantTableLiveStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantTableLiveStatusDto>>>> GetPublicLiveStatus(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var statuses = await _bookingService.GetPublicRestaurantLiveTableStatusAsync(restaurantId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantTableLiveStatusDto>>
            {
                Message = "Public live table status loaded successfully.",
                Data = statuses
            });
        }
        catch (BookingFlowServiceException exception)
        {
            return BuildErrorResponse<IReadOnlyList<PublicRestaurantTableLiveStatusDto>>(exception);
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
