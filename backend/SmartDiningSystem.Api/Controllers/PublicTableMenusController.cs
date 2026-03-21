using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.DTOs.TableOrdering;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/public/tables")]
public class PublicTableMenusController : ControllerBase
{
    private readonly IRestaurantTableAccessService _restaurantTableAccessService;
    private readonly IPublicTableMenuService _publicTableMenuService;

    public PublicTableMenusController(
        IRestaurantTableAccessService restaurantTableAccessService,
        IPublicTableMenuService publicTableMenuService)
    {
        _restaurantTableAccessService = restaurantTableAccessService;
        _publicTableMenuService = publicTableMenuService;
    }

    [HttpGet("{tableToken}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<ResolvedRestaurantTableDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<ResolvedRestaurantTableDto>>> ResolveTable(
        string tableToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var table = await _restaurantTableAccessService.ResolveTableAsync(tableToken, cancellationToken);
            return Ok(new ApiSuccessResponseDto<ResolvedRestaurantTableDto>
            {
                Message = "Table resolved successfully.",
                Data = table
            });
        }
        catch (TableOrderingServiceException exception)
        {
            return BuildErrorResponse<ResolvedRestaurantTableDto>(exception);
        }
    }

    [HttpGet("{tableToken}/menu")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<PublicTableMenuResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<PublicTableMenuResponseDto>>> GetMenu(
        string tableToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var menu = await _publicTableMenuService.GetPublicMenuAsync(tableToken, cancellationToken);
            return Ok(new ApiSuccessResponseDto<PublicTableMenuResponseDto>
            {
                Message = "Public table menu loaded successfully.",
                Data = menu
            });
        }
        catch (TableOrderingServiceException exception)
        {
            return BuildErrorResponse<PublicTableMenuResponseDto>(exception);
        }
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
