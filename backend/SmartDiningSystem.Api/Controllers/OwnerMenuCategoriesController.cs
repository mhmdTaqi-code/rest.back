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
[Route("api/owner/restaurants/{restaurantId:guid}/menu/categories")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "RestaurantOwner")]
public class OwnerMenuCategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    public OwnerMenuCategoriesController(ICategoryService categoryService) => _categoryService = categoryService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<MenuCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<MenuCategoryDto>>>> GetCategories(Guid restaurantId, CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null) return Unauthorized(BuildUnauthorizedResponse());
        try
        {
            var categories = await _categoryService.GetOwnerCategoriesAsync(ownerId.Value, restaurantId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<MenuCategoryDto>> { Message = "Menu categories loaded successfully.", Data = categories });
        }
        catch (MenuManagementServiceException exception) { return BuildErrorResponse<IReadOnlyList<MenuCategoryDto>>(exception); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<MenuCategoryDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiSuccessResponseDto<MenuCategoryDto>>> CreateCategory(Guid restaurantId, [FromBody] CreateCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null) return Unauthorized(BuildUnauthorizedResponse());
        try
        {
            var category = await _categoryService.CreateCategoryAsync(ownerId.Value, restaurantId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiSuccessResponseDto<MenuCategoryDto> { Message = "Menu category created successfully.", Data = category });
        }
        catch (MenuManagementServiceException exception) { return BuildErrorResponse<MenuCategoryDto>(exception); }
    }

    [HttpPut("{categoryId:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<MenuCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<MenuCategoryDto>>> UpdateCategory(Guid restaurantId, Guid categoryId, [FromBody] UpdateCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null) return Unauthorized(BuildUnauthorizedResponse());
        try
        {
            var category = await _categoryService.UpdateCategoryAsync(ownerId.Value, restaurantId, categoryId, request, cancellationToken);
            return Ok(new ApiSuccessResponseDto<MenuCategoryDto> { Message = "Menu category updated successfully.", Data = category });
        }
        catch (MenuManagementServiceException exception) { return BuildErrorResponse<MenuCategoryDto>(exception); }
    }

    [HttpDelete("{categoryId:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<object>>> DeleteCategory(Guid restaurantId, Guid categoryId, CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null) return Unauthorized(BuildUnauthorizedResponse());
        try
        {
            await _categoryService.DeleteCategoryAsync(ownerId.Value, restaurantId, categoryId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<object> { Message = "Menu category deleted successfully." });
        }
        catch (MenuManagementServiceException exception) { return BuildErrorResponse<object>(exception); }
    }

    private Guid? GetOwnerId() => Guid.TryParse(User.FindFirstValue("userId"), out var parsed) ? parsed : null;
    private ApiErrorResponseDto BuildUnauthorizedResponse() => new() { Message = "Unauthorized owner context.", TraceId = HttpContext.TraceIdentifier };
    private ActionResult<ApiSuccessResponseDto<T>> BuildErrorResponse<T>(MenuManagementServiceException exception) => StatusCode(exception.StatusCode, new ApiErrorResponseDto { Message = exception.Message, Errors = exception.Errors, TraceId = HttpContext.TraceIdentifier });
}
