using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.DTOs.TableOrdering;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/public/restaurants/{restaurantId:guid}/tables/{tableId:guid}")]
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

    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<ResolvedRestaurantTableDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<ResolvedRestaurantTableDto>>> ResolveTable(
        Guid restaurantId,
        Guid tableId,
        CancellationToken cancellationToken)
    {
        try
        {
            var table = await _restaurantTableAccessService.ResolveTableAsync(restaurantId, tableId, cancellationToken);
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

    [HttpGet("menu")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<PublicTableMenuResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<PublicTableMenuResponseDto>>> GetMenu(
        Guid restaurantId,
        Guid tableId,
        CancellationToken cancellationToken)
    {
        try
        {
            var menu = await _publicTableMenuService.GetPublicMenuAsync(restaurantId, tableId, cancellationToken);
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
