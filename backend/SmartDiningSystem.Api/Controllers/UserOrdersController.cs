using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.DTOs.Orders;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/user/orders")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UserOrdersController : ControllerBase
{
    private readonly IUserOrderTrackingService _userOrderTrackingService;

    public UserOrdersController(IUserOrderTrackingService userOrderTrackingService)
    {
        _userOrderTrackingService = userOrderTrackingService;
    }

    [HttpGet("{orderId:guid}/status")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<UserOrderStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<UserOrderStatusDto>>> GetOrderStatus(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponseDto
            {
                Message = "Unauthorized user context.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        try
        {
            var order = await _userOrderTrackingService.GetOrderStatusAsync(userId.Value, orderId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<UserOrderStatusDto>
            {
                Message = "Order status loaded successfully.",
                Data = order
            });
        }
        catch (UserOrderTrackingServiceException exception)
        {
            return StatusCode(exception.StatusCode, new ApiErrorResponseDto
            {
                Message = exception.Message,
                Errors = exception.Errors,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    private Guid? GetUserId()
    {
        var userId = User.FindFirstValue("userId");
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }
}
