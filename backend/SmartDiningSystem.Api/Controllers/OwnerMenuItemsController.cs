using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.DTOs.MenuManagement;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/owner/menu/items")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "RestaurantOwner")]
public class OwnerMenuItemsController : ControllerBase
{
    private readonly IMenuItemService _menuItemService;

    public OwnerMenuItemsController(IMenuItemService menuItemService)
    {
        _menuItemService = menuItemService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<MenuItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<MenuItemDto>>>> GetMenuItems(
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var items = await _menuItemService.GetOwnerMenuItemsAsync(ownerId.Value, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<MenuItemDto>>
            {
                Message = "Menu items loaded successfully.",
                Data = items
            });
        }
        catch (MenuManagementServiceException exception)
        {
            return BuildErrorResponse<IReadOnlyList<MenuItemDto>>(exception);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<MenuItemDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiSuccessResponseDto<MenuItemDto>>> CreateMenuItem(
        [FromBody] CreateMenuItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var item = await _menuItemService.CreateMenuItemAsync(ownerId.Value, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiSuccessResponseDto<MenuItemDto>
            {
                Message = "Menu item created successfully.",
                Data = item
            });
        }
        catch (MenuManagementServiceException exception)
        {
            return BuildErrorResponse<MenuItemDto>(exception);
        }
    }

    [HttpPut("{menuItemId:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<MenuItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<MenuItemDto>>> UpdateMenuItem(
        Guid menuItemId,
        [FromBody] UpdateMenuItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var item = await _menuItemService.UpdateMenuItemAsync(ownerId.Value, menuItemId, request, cancellationToken);
            return Ok(new ApiSuccessResponseDto<MenuItemDto>
            {
                Message = "Menu item updated successfully.",
                Data = item
            });
        }
        catch (MenuManagementServiceException exception)
        {
            return BuildErrorResponse<MenuItemDto>(exception);
        }
    }

    [HttpPut("{menuItemId:guid}/highlight")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<MenuItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<MenuItemDto>>> SetHighlight(
        Guid menuItemId,
        [FromBody] SetMenuItemHighlightRequestDto request,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var item = await _menuItemService.SetMenuItemHighlightAsync(ownerId.Value, menuItemId, request, cancellationToken);
            return Ok(new ApiSuccessResponseDto<MenuItemDto>
            {
                Message = "Menu item highlight updated successfully.",
                Data = item
            });
        }
        catch (MenuManagementServiceException exception)
        {
            return BuildErrorResponse<MenuItemDto>(exception);
        }
    }

    [HttpDelete("{menuItemId:guid}/highlight")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<MenuItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<MenuItemDto>>> RemoveHighlight(
        Guid menuItemId,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var item = await _menuItemService.RemoveMenuItemHighlightAsync(ownerId.Value, menuItemId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<MenuItemDto>
            {
                Message = "Menu item highlight removed successfully.",
                Data = item
            });
        }
        catch (MenuManagementServiceException exception)
        {
            return BuildErrorResponse<MenuItemDto>(exception);
        }
    }

    [HttpPatch("{menuItemId:guid}/availability")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<MenuItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<MenuItemDto>>> ToggleAvailability(
        Guid menuItemId,
        [FromBody] ToggleMenuItemAvailabilityRequestDto request,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var item = await _menuItemService.ToggleAvailabilityAsync(ownerId.Value, menuItemId, request, cancellationToken);
            return Ok(new ApiSuccessResponseDto<MenuItemDto>
            {
                Message = "Menu item availability updated successfully.",
                Data = item
            });
        }
        catch (MenuManagementServiceException exception)
        {
            return BuildErrorResponse<MenuItemDto>(exception);
        }
    }

    [HttpDelete("{menuItemId:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<object>>> DeleteMenuItem(
        Guid menuItemId,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            await _menuItemService.DeleteMenuItemAsync(ownerId.Value, menuItemId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<object>
            {
                Message = "Menu item deleted successfully."
            });
        }
        catch (MenuManagementServiceException exception)
        {
            return BuildErrorResponse<object>(exception);
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

    private ActionResult<ApiSuccessResponseDto<T>> BuildErrorResponse<T>(MenuManagementServiceException exception)
    {
        return StatusCode(exception.StatusCode, new ApiErrorResponseDto
        {
            Message = exception.Message,
            Errors = exception.Errors,
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
