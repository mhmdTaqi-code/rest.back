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
[Route("api/owner/table-sessions")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "RestaurantOwner")]
public class OwnerTableSessionsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public OwnerTableSessionsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost("{sessionId:guid}/checkout")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<OwnerCheckoutTableSessionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<OwnerCheckoutTableSessionResponseDto>>> Checkout(
        Guid sessionId,
        [FromBody] OwnerCheckoutTableSessionRequestDto? request,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var result = await _bookingService.CheckoutTableSessionAsync(
                ownerId.Value,
                sessionId,
                request ?? new OwnerCheckoutTableSessionRequestDto(),
                cancellationToken);

            return Ok(new ApiSuccessResponseDto<OwnerCheckoutTableSessionResponseDto>
            {
                Message = "Table session checked out successfully.",
                Data = result
            });
        }
        catch (BookingFlowServiceException exception)
        {
            return BuildErrorResponse<OwnerCheckoutTableSessionResponseDto>(exception);
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
