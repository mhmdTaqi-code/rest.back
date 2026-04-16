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
    [ProducesResponseType(typeof(ApiResponseDto<TableAccessScanResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<TableAccessScanResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<TableAccessScanResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<TableAccessScanResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponseDto<TableAccessScanResponseDto>), StatusCodes.Status409Conflict)]
    public Task<ActionResult<ApiResponseDto<TableAccessScanResponseDto>>> Scan(
        [FromBody] TableAccessScanRequestDto request,
        CancellationToken cancellationToken) =>
        ScanCoreAsync(request, cancellationToken);

    private Guid? GetUserId()
    {
        var userId = User.FindFirstValue("userId");
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private async Task<ActionResult<ApiResponseDto<TableAccessScanResponseDto>>> ScanCoreAsync(
        TableAccessScanRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var decision = await _tableAccessFlowService.ProcessScanAsync(userId, request, cancellationToken);
            var statusCode = ResolveStatusCode(decision);
            return StatusCode(statusCode, new ApiResponseDto<TableAccessScanResponseDto>
            {
                Success = statusCode < StatusCodes.Status400BadRequest,
                Message = decision.Message,
                Data = decision,
                Errors = null
            });
        }
        catch (BookingFlowServiceException exception)
        {
            return BuildErrorResponse(exception);
        }
    }

    private static int ResolveStatusCode(TableAccessScanResponseDto decision)
    {
        return decision.ResultType switch
        {
            TableAccessScanResultType.InvalidRequest => StatusCodes.Status400BadRequest,
            TableAccessScanResultType.Unauthorized => StatusCodes.Status401Unauthorized,
            TableAccessScanResultType.NotFound => StatusCodes.Status404NotFound,
            TableAccessScanResultType.Occupied => StatusCodes.Status409Conflict,
            TableAccessScanResultType.Reserved => StatusCodes.Status409Conflict,
            TableAccessScanResultType.Blocked => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status200OK
        };
    }

    private ActionResult<ApiResponseDto<TableAccessScanResponseDto>> BuildErrorResponse(BookingFlowServiceException exception)
    {
        return StatusCode(exception.StatusCode, new ApiResponseDto<TableAccessScanResponseDto>
        {
            Success = false,
            Message = exception.Message,
            Data = null,
            Errors = exception.Errors,
        });
    }
}
