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
[Route("api/owner/restaurants/{restaurantId:guid}/orders")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "RestaurantOwner")]
public class OwnerOrdersController : ControllerBase
{
    private readonly IOwnerOrderWorkflowService _ownerOrderWorkflowService;
    public OwnerOrdersController(IOwnerOrderWorkflowService ownerOrderWorkflowService) => _ownerOrderWorkflowService = ownerOrderWorkflowService;

    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<OwnerActiveOrderDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<OwnerActiveOrderDto>>>> GetActiveOrders(Guid restaurantId, CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId(); if (ownerId is null) return Unauthorized(BuildUnauthorizedResponse());
        try { var orders = await _ownerOrderWorkflowService.GetActiveOrdersAsync(ownerId.Value, restaurantId, cancellationToken); return Ok(new ApiSuccessResponseDto<IReadOnlyList<OwnerActiveOrderDto>> { Message = "Active restaurant orders loaded successfully.", Data = orders }); }
        catch (OwnerOrderWorkflowServiceException exception) { return BuildErrorResponse<IReadOnlyList<OwnerActiveOrderDto>>(exception); }
    }

    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<OwnerOrderDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<OwnerOrderDetailDto>>> GetOrderDetails(Guid restaurantId, Guid orderId, CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId(); if (ownerId is null) return Unauthorized(BuildUnauthorizedResponse());
        try { var order = await _ownerOrderWorkflowService.GetOrderDetailsAsync(ownerId.Value, restaurantId, orderId, cancellationToken); return Ok(new ApiSuccessResponseDto<OwnerOrderDetailDto> { Message = "Restaurant order details loaded successfully.", Data = order }); }
        catch (OwnerOrderWorkflowServiceException exception) { return BuildErrorResponse<OwnerOrderDetailDto>(exception); }
    }

    [HttpPatch("{orderId:guid}/status")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<OwnerOrderDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<OwnerOrderDetailDto>>> UpdateOrderStatus(Guid restaurantId, Guid orderId, [FromBody] UpdateOrderStatusRequestDto request, CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId(); if (ownerId is null) return Unauthorized(BuildUnauthorizedResponse());
        try { var order = await _ownerOrderWorkflowService.UpdateOrderStatusAsync(ownerId.Value, restaurantId, orderId, request, cancellationToken); return Ok(new ApiSuccessResponseDto<OwnerOrderDetailDto> { Message = "Restaurant order status updated successfully.", Data = order }); }
        catch (OwnerOrderWorkflowServiceException exception) { return BuildErrorResponse<OwnerOrderDetailDto>(exception); }
    }

    private Guid? GetOwnerId() => Guid.TryParse(User.FindFirstValue("userId"), out var parsed) ? parsed : null;
    private ApiErrorResponseDto BuildUnauthorizedResponse() => new() { Message = "Unauthorized owner context.", TraceId = HttpContext.TraceIdentifier };
    private ActionResult<ApiSuccessResponseDto<T>> BuildErrorResponse<T>(OwnerOrderWorkflowServiceException exception) => StatusCode(exception.StatusCode, new ApiErrorResponseDto { Message = exception.Message, Errors = exception.Errors, TraceId = HttpContext.TraceIdentifier });
}
