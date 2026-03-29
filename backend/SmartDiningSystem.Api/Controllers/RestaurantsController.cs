using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.DTOs.Restaurants;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/restaurants")]
public class RestaurantsController : ControllerBase
{
    private readonly IRestaurantQueryService _restaurantQueryService;
    private readonly IRestaurantRecommendationService _restaurantRecommendationService;

    public RestaurantsController(
        IRestaurantQueryService restaurantQueryService,
        IRestaurantRecommendationService restaurantRecommendationService)
    {
        _restaurantQueryService = restaurantQueryService;
        _restaurantRecommendationService = restaurantRecommendationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantSummaryDto>>>> GetPublicRestaurants(
        CancellationToken cancellationToken)
    {
        var restaurants = await _restaurantQueryService.GetPublicRestaurantsAsync(cancellationToken);
        return Ok(new ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantSummaryDto>>
        {
            Message = "Approved restaurants loaded successfully.",
            Data = restaurants
        });
    }

    [HttpGet("{restaurantId:guid}/tables")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantTableDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantTableDto>>>> GetRestaurantTables(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var tables = await _restaurantQueryService.GetTablesByRestaurantIdAsync(restaurantId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantTableDto>>
            {
                Success = true,
                Data = tables
            });
        }
        catch (RestaurantQueryServiceException exception)
        {
            return BuildErrorResponse<IReadOnlyList<PublicRestaurantTableDto>>(exception);
        }
    }

    [HttpGet("{restaurantId:guid}/menu")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantMenuItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantMenuItemDto>>>> GetRestaurantMenu(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var menu = await _restaurantQueryService.GetMenuByRestaurantIdAsync(restaurantId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantMenuItemDto>>
            {
                Success = true,
                Data = menu
            });
        }
        catch (RestaurantQueryServiceException exception)
        {
            return BuildErrorResponse<IReadOnlyList<PublicRestaurantMenuItemDto>>(exception);
        }
    }

    [HttpGet("recommendations")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<RecommendedRestaurantDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<RecommendedRestaurantDto>>>> GetRecommendations(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var recommendations = await _restaurantRecommendationService.GetRecommendationsAsync(userId.Value, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<RecommendedRestaurantDto>>
            {
                Message = "Restaurant recommendations loaded successfully.",
                Data = recommendations
            });
        }
        catch (RestaurantRecommendationServiceException exception)
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

    private ActionResult<ApiSuccessResponseDto<T>> BuildErrorResponse<T>(RestaurantQueryServiceException exception)
    {
        return StatusCode(exception.StatusCode, new ApiErrorResponseDto
        {
            Message = exception.Message,
            Errors = exception.Errors,
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
