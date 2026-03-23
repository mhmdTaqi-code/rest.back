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
[Route("api/owner/menu/categories")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "RestaurantOwner")]
public class OwnerMenuCategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public OwnerMenuCategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<MenuCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<MenuCategoryDto>>>> GetCategories(
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var categories = await _categoryService.GetOwnerCategoriesAsync(ownerId.Value, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<MenuCategoryDto>>
            {
                Message = "Menu categories loaded successfully.",
                Data = categories
            });
        }
        catch (MenuManagementServiceException exception)
        {
            return BuildErrorResponse<IReadOnlyList<MenuCategoryDto>>(exception);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<MenuCategoryDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiSuccessResponseDto<MenuCategoryDto>>> CreateCategory(
        [FromBody] CreateCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var category = await _categoryService.CreateCategoryAsync(ownerId.Value, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiSuccessResponseDto<MenuCategoryDto>
            {
                Message = "Menu category created successfully.",
                Data = category
            });
        }
        catch (MenuManagementServiceException exception)
        {
            return BuildErrorResponse<MenuCategoryDto>(exception);
        }
    }

    [HttpPut("{categoryId:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<MenuCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<MenuCategoryDto>>> UpdateCategory(
        Guid categoryId,
        [FromBody] UpdateCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var category = await _categoryService.UpdateCategoryAsync(ownerId.Value, categoryId, request, cancellationToken);
            return Ok(new ApiSuccessResponseDto<MenuCategoryDto>
            {
                Message = "Menu category updated successfully.",
                Data = category
            });
        }
        catch (MenuManagementServiceException exception)
        {
            return BuildErrorResponse<MenuCategoryDto>(exception);
        }
    }

    [HttpDelete("{categoryId:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<object>>> DeleteCategory(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            await _categoryService.DeleteCategoryAsync(ownerId.Value, categoryId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<object>
            {
                Message = "Menu category deleted successfully."
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
