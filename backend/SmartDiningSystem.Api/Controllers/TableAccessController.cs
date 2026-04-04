using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.DTOs.TableAccess;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/table-access")]
public class TableAccessController : ControllerBase
{
    private readonly ITableAccessFlowService _tableAccessFlowService;

    public TableAccessController(ITableAccessFlowService tableAccessFlowService)
    {
        _tableAccessFlowService = tableAccessFlowService;
    }

    [HttpPost("scan")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<TableAccessScanResponseDto>), StatusCodes.Status200OK)]
    public Task<ActionResult<ApiSuccessResponseDto<TableAccessScanResponseDto>>> Scan(
        [FromBody] TableAccessScanRequestDto request,
        CancellationToken cancellationToken) =>
        ScanCoreAsync(request, cancellationToken);

    private Guid? GetUserId()
    {
        var userId = User.FindFirstValue("userId");
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private async Task<ActionResult<ApiSuccessResponseDto<TableAccessScanResponseDto>>> ScanCoreAsync(
        TableAccessScanRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var decision = await _tableAccessFlowService.ProcessScanAsync(userId, request, cancellationToken);
            return Ok(new ApiSuccessResponseDto<TableAccessScanResponseDto>
            {
                Message = decision.Message,
                Data = decision
            });
        }
        catch (BookingFlowServiceException exception)
        {
            return BuildErrorResponse<TableAccessScanResponseDto>(exception);
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
