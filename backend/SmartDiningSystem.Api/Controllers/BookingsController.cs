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
[Route("api/bookings")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<BookingDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<BookingDto>>>> GetMy(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var bookings = await _bookingService.GetMyBookingsAsync(userId.Value, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<BookingDto>>
            {
                Message = "Bookings loaded successfully.",
                Data = bookings
            });
        }
        catch (BookingFlowServiceException exception)
        {
            return BuildErrorResponse<IReadOnlyList<BookingDto>>(exception);
        }
    }

    [HttpGet("{bookingId:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<BookingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<BookingDto>>> Get(Guid bookingId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var booking = await _bookingService.GetMyBookingAsync(userId.Value, bookingId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<BookingDto>
            {
                Message = "Booking loaded successfully.",
                Data = booking
            });
        }
        catch (BookingFlowServiceException exception)
        {
            return BuildErrorResponse<BookingDto>(exception);
        }
    }

    [HttpDelete("{bookingId:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<BookingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<BookingDto>>> Delete(Guid bookingId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var booking = await _bookingService.CancelBookingAsync(userId.Value, bookingId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<BookingDto>
            {
                Message = "Booking cancelled successfully.",
                Data = booking
            });
        }
        catch (BookingFlowServiceException exception)
        {
            return BuildErrorResponse<BookingDto>(exception);
        }
    }

    [HttpPost("{bookingId:guid}/check-in")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<BookingCheckInResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<BookingCheckInResponseDto>>> CheckIn(Guid bookingId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var result = await _bookingService.CheckInAsync(userId.Value, bookingId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<BookingCheckInResponseDto>
            {
                Message = "Booking checked in successfully.",
                Data = result
            });
        }
        catch (BookingFlowServiceException exception)
        {
            return BuildErrorResponse<BookingCheckInResponseDto>(exception);
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
