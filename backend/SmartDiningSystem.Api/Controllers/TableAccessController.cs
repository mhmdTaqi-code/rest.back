using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SmartDiningSystem.Application.DTOs.Bookings;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/table-access")]
public class TableAccessController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public TableAccessController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("scan")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<TableAccessDecisionDto>), StatusCodes.Status200OK)]
    public Task<ActionResult<ApiSuccessResponseDto<TableAccessDecisionDto>>> Scan(
        [FromQuery, BindRequired] Guid tableId,
        CancellationToken cancellationToken) =>
        ScanCoreAsync(tableId, cancellationToken);

    private Guid? GetUserId()
    {
        var userId = User.FindFirstValue("userId");
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private async Task<ActionResult<ApiSuccessResponseDto<TableAccessDecisionDto>>> ScanCoreAsync(
        Guid tableId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var decision = await _bookingService.ScanTableAccessAsync(userId, tableId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<TableAccessDecisionDto>
            {
                Message = "Table access evaluated successfully.",
                Data = decision
            });
        }
        catch (BookingFlowServiceException exception)
        {
            return BuildErrorResponse<TableAccessDecisionDto>(exception);
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
