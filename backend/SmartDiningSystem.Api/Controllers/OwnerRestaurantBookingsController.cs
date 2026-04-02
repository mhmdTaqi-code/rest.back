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
[Route("api/owner/restaurants/{restaurantId:guid}")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "RestaurantOwner")]
public class OwnerRestaurantBookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public OwnerRestaurantBookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("bookings")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<OwnerRestaurantBookingDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<OwnerRestaurantBookingDto>>>> GetBookings(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var bookings = await _bookingService.GetOwnerBookingsAsync(ownerId.Value, restaurantId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<OwnerRestaurantBookingDto>>
            {
                Message = "Owner bookings loaded successfully.",
                Data = bookings
            });
        }
        catch (BookingFlowServiceException exception)
        {
            return BuildErrorResponse<IReadOnlyList<OwnerRestaurantBookingDto>>(exception);
        }
    }

    [HttpGet("tables/live-status")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<OwnerRestaurantTableLiveStatusDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<OwnerRestaurantTableLiveStatusDto>>>> GetLiveStatus(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var statuses = await _bookingService.GetOwnerLiveTableStatusAsync(ownerId.Value, restaurantId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<OwnerRestaurantTableLiveStatusDto>>
            {
                Message = "Owner live table status loaded successfully.",
                Data = statuses
            });
        }
        catch (BookingFlowServiceException exception)
        {
            return BuildErrorResponse<IReadOnlyList<OwnerRestaurantTableLiveStatusDto>>(exception);
        }
    }

    private Guid? GetOwnerId()
    {
        var userId = User.FindFirstValue("userId");
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private ApiErrorResponseDto BuildUnauthorizedResponse()
    {
        return new ApiErrorResponseDto
        {
            Message = "Unauthorized owner context.",
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
