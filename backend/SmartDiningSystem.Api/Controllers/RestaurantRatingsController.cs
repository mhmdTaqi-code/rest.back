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
[Route("api/restaurants/{restaurantId:guid}")]
public class RestaurantRatingsController : ControllerBase
{
    private readonly IRestaurantRatingService _restaurantRatingService;

    public RestaurantRatingsController(IRestaurantRatingService restaurantRatingService)
    {
        _restaurantRatingService = restaurantRatingService;
    }

    [HttpPut("rating")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<RestaurantRatingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<RestaurantRatingDto>>> UpsertRating(
        Guid restaurantId,
        [FromBody] SubmitRestaurantRatingRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var rating = await _restaurantRatingService.UpsertRatingAsync(
                userId.Value,
                restaurantId,
                request,
                cancellationToken);

            return Ok(new ApiSuccessResponseDto<RestaurantRatingDto>
            {
                Message = "Restaurant rating saved successfully.",
                Data = rating
            });
        }
        catch (RestaurantRatingServiceException exception)
        {
            return BuildErrorResponse<RestaurantRatingDto>(exception);
        }
    }

    [HttpGet("rating/me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<RestaurantRatingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<RestaurantRatingDto?>>> GetCurrentUserRating(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var rating = await _restaurantRatingService.GetUserRatingAsync(
                userId.Value,
                restaurantId,
                cancellationToken);

            return Ok(new ApiSuccessResponseDto<RestaurantRatingDto?>
            {
                Message = "Restaurant rating loaded successfully.",
                Data = rating
            });
        }
        catch (RestaurantRatingServiceException exception)
        {
            return BuildErrorResponse<RestaurantRatingDto?>(exception);
        }
    }

    [HttpGet("rating-summary")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<RestaurantRatingSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<RestaurantRatingSummaryDto>>> GetRatingSummary(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _restaurantRatingService.GetRatingSummaryAsync(restaurantId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<RestaurantRatingSummaryDto>
            {
                Message = "Restaurant rating summary loaded successfully.",
                Data = summary
            });
        }
        catch (RestaurantRatingServiceException exception)
        {
            return BuildErrorResponse<RestaurantRatingSummaryDto>(exception);
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

    private ActionResult<ApiSuccessResponseDto<T>> BuildErrorResponse<T>(RestaurantRatingServiceException exception)
    {
        return StatusCode(exception.StatusCode, new ApiErrorResponseDto
        {
            Message = exception.Message,
            Errors = exception.Errors,
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
