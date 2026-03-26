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
[Route("api/menu-items/recommendations")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MenuRecommendationsController : ControllerBase
{
    private readonly IMenuRecommendationService _menuRecommendationService;

    public MenuRecommendationsController(IMenuRecommendationService menuRecommendationService)
    {
        _menuRecommendationService = menuRecommendationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<RecommendedMenuItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<RecommendedMenuItemDto>>>> GetRecommendations(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var recommendations = await _menuRecommendationService.GetRecommendationsAsync(userId.Value, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<RecommendedMenuItemDto>>
            {
                Message = "Recommended menu items loaded successfully.",
                Data = recommendations
            });
        }
        catch (MenuRecommendationServiceException exception)
        {
            return StatusCode(exception.StatusCode, new ApiErrorResponseDto
            {
                Message = exception.Message,
                Errors = exception.Errors,
                TraceId = HttpContext.TraceIdentifier
            });
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
}
