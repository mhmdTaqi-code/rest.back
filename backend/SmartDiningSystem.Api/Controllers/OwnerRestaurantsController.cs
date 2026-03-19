using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.DTOs.Restaurants;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/owner/restaurants")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "RestaurantOwner")]
public class OwnerRestaurantsController : ControllerBase
{
    private readonly IRestaurantQueryService _restaurantQueryService;

    public OwnerRestaurantsController(IRestaurantQueryService restaurantQueryService)
    {
        _restaurantQueryService = restaurantQueryService;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<OwnerRestaurantStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<OwnerRestaurantStatusDto>>> GetMyRestaurantStatus(
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("userId"), out var ownerId))
        {
            return Unauthorized(new ApiErrorResponseDto
            {
                Message = "Unauthorized owner context.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        try
        {
            var result = await _restaurantQueryService.GetOwnerRestaurantStatusAsync(ownerId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<OwnerRestaurantStatusDto>
            {
                Message = "Restaurant status loaded successfully.",
                Data = result
            });
        }
        catch (AuthServiceException exception)
        {
            return StatusCode(exception.StatusCode, new ApiErrorResponseDto
            {
                Message = exception.Message,
                Errors = exception.Errors,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
