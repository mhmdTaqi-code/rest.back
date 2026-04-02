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
[Route("api/table-sessions/{sessionId:guid}/orders")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TableSessionsController : ControllerBase
{
    private readonly ITableSessionOrderService _tableSessionOrderService;

    public TableSessionsController(ITableSessionOrderService tableSessionOrderService)
    {
        _tableSessionOrderService = tableSessionOrderService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<SubmittedTableOrderResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiSuccessResponseDto<SubmittedTableOrderResponseDto>>> SubmitOrder(
        Guid sessionId,
        [FromBody] SubmitTableOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var result = await _tableSessionOrderService.SubmitOrderAsync(userId.Value, sessionId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiSuccessResponseDto<SubmittedTableOrderResponseDto>
            {
                Message = "Order submitted successfully.",
                Data = result
            });
        }
        catch (BookingFlowServiceException exception)
        {
            return BuildErrorResponse<SubmittedTableOrderResponseDto>(exception);
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
