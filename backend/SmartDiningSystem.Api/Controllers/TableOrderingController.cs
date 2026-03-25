using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.DTOs.TableOrdering;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/table-ordering/restaurants/{restaurantId:guid}/tables/{tableId:guid}")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TableOrderingController : ControllerBase
{
    private readonly ITableCartService _tableCartService;
    private readonly ITableOrderService _tableOrderService;

    public TableOrderingController(
        ITableCartService tableCartService,
        ITableOrderService tableOrderService)
    {
        _tableCartService = tableCartService;
        _tableOrderService = tableOrderService;
    }

    [HttpGet("cart")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<TableCartResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<TableCartResponseDto>>> GetCurrentCart(
        Guid restaurantId,
        Guid tableId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrUnauthorized();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var cart = await _tableCartService.GetCurrentCartAsync(userId.Value, restaurantId, tableId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<TableCartResponseDto>
            {
                Message = "Current cart loaded successfully.",
                Data = cart
            });
        }
        catch (TableOrderingServiceException exception)
        {
            return BuildErrorResponse<TableCartResponseDto>(exception);
        }
    }

    [HttpPost("cart/items")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<TableCartResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiSuccessResponseDto<TableCartResponseDto>>> AddItem(
        Guid restaurantId,
        Guid tableId,
        [FromBody] AddCartItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrUnauthorized();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var cart = await _tableCartService.AddItemAsync(userId.Value, restaurantId, tableId, request, cancellationToken);
            return Ok(new ApiSuccessResponseDto<TableCartResponseDto>
            {
                Message = "Item added to cart successfully.",
                Data = cart
            });
        }
        catch (TableOrderingServiceException exception)
        {
            return BuildErrorResponse<TableCartResponseDto>(exception);
        }
    }

    [HttpPut("cart/items/{cartItemId:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<TableCartResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<TableCartResponseDto>>> UpdateItem(
        Guid restaurantId,
        Guid tableId,
        Guid cartItemId,
        [FromBody] UpdateCartItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrUnauthorized();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var cart = await _tableCartService.UpdateItemAsync(userId.Value, restaurantId, tableId, cartItemId, request, cancellationToken);
            return Ok(new ApiSuccessResponseDto<TableCartResponseDto>
            {
                Message = "Cart item updated successfully.",
                Data = cart
            });
        }
        catch (TableOrderingServiceException exception)
        {
            return BuildErrorResponse<TableCartResponseDto>(exception);
        }
    }

    [HttpDelete("cart/items/{cartItemId:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<TableCartResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<TableCartResponseDto>>> RemoveItem(
        Guid restaurantId,
        Guid tableId,
        Guid cartItemId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrUnauthorized();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var cart = await _tableCartService.RemoveItemAsync(userId.Value, restaurantId, tableId, cartItemId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<TableCartResponseDto>
            {
                Message = "Cart item removed successfully.",
                Data = cart
            });
        }
        catch (TableOrderingServiceException exception)
        {
            return BuildErrorResponse<TableCartResponseDto>(exception);
        }
    }

    [HttpPost("orders")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<SubmittedTableOrderResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiSuccessResponseDto<SubmittedTableOrderResponseDto>>> SubmitOrder(
        Guid restaurantId,
        Guid tableId,
        [FromBody] SubmitTableOrderRequestDto? request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrUnauthorized();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var order = await _tableOrderService.SubmitOrderAsync(
                userId.Value,
                restaurantId,
                tableId,
                request ?? new SubmitTableOrderRequestDto(),
                cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiSuccessResponseDto<SubmittedTableOrderResponseDto>
            {
                Message = "Order submitted successfully.",
                Data = order
            });
        }
        catch (TableOrderingServiceException exception)
        {
            return BuildErrorResponse<SubmittedTableOrderResponseDto>(exception);
        }
    }

    private Guid? GetUserIdOrUnauthorized()
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

    private ActionResult<ApiSuccessResponseDto<T>> BuildErrorResponse<T>(TableOrderingServiceException exception)
    {
        return StatusCode(exception.StatusCode, new ApiErrorResponseDto
        {
            Message = exception.Message,
            Errors = exception.Errors,
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
