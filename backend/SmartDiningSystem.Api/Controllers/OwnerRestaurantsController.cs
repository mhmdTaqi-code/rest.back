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
[Route("api/owner/restaurants")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "RestaurantOwner")]
public class OwnerRestaurantsController : ControllerBase
{
    private readonly IOwnerRestaurantProfileService _ownerRestaurantProfileService;

    public OwnerRestaurantsController(IOwnerRestaurantProfileService ownerRestaurantProfileService)
    {
        _ownerRestaurantProfileService = ownerRestaurantProfileService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<OwnerRestaurantStatusDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<OwnerRestaurantStatusDto>>>> GetRestaurants(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("userId"), out var ownerId))
        {
            return Unauthorized(new ApiErrorResponseDto { Message = "Unauthorized owner context.", TraceId = HttpContext.TraceIdentifier });
        }

        try
        {
            var result = await _ownerRestaurantProfileService.GetRestaurantsAsync(ownerId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<OwnerRestaurantStatusDto>> { Message = "Owner restaurants loaded successfully.", Data = result });
        }
        catch (OwnerRestaurantProfileServiceException exception)
        {
            return StatusCode(exception.StatusCode, new ApiErrorResponseDto { Message = exception.Message, Errors = exception.Errors, TraceId = HttpContext.TraceIdentifier });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<OwnerRestaurantStatusDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiSuccessResponseDto<OwnerRestaurantStatusDto>>> CreateRestaurant([FromBody] CreateOwnerRestaurantRequestDto request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("userId"), out var ownerId))
        {
            return Unauthorized(new ApiErrorResponseDto { Message = "Unauthorized owner context.", TraceId = HttpContext.TraceIdentifier });
        }

        try
        {
            var result = await _ownerRestaurantProfileService.CreateRestaurantAsync(ownerId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiSuccessResponseDto<OwnerRestaurantStatusDto> { Message = "Restaurant created successfully.", Data = result });
        }
        catch (OwnerRestaurantProfileServiceException exception)
        {
            return StatusCode(exception.StatusCode, new ApiErrorResponseDto { Message = exception.Message, Errors = exception.Errors, TraceId = HttpContext.TraceIdentifier });
        }
    }

    [HttpGet("{restaurantId:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<OwnerRestaurantStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<OwnerRestaurantStatusDto>>> GetRestaurant(Guid restaurantId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("userId"), out var ownerId))
        {
            return Unauthorized(new ApiErrorResponseDto { Message = "Unauthorized owner context.", TraceId = HttpContext.TraceIdentifier });
        }

        try
        {
            var result = await _ownerRestaurantProfileService.GetRestaurantAsync(ownerId, restaurantId, cancellationToken);
            return Ok(new ApiSuccessResponseDto<OwnerRestaurantStatusDto> { Message = "Restaurant loaded successfully.", Data = result });
        }
        catch (OwnerRestaurantProfileServiceException exception)
        {
            return StatusCode(exception.StatusCode, new ApiErrorResponseDto { Message = exception.Message, Errors = exception.Errors, TraceId = HttpContext.TraceIdentifier });
        }
    }

    [HttpPut("{restaurantId:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<OwnerRestaurantStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<OwnerRestaurantStatusDto>>> UpdateRestaurant(Guid restaurantId, [FromBody] UpdateOwnerRestaurantRequestDto request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("userId"), out var ownerId))
        {
            return Unauthorized(new ApiErrorResponseDto { Message = "Unauthorized owner context.", TraceId = HttpContext.TraceIdentifier });
        }

        try
        {
            var result = await _ownerRestaurantProfileService.UpdateRestaurantAsync(ownerId, restaurantId, request, cancellationToken);
            return Ok(new ApiSuccessResponseDto<OwnerRestaurantStatusDto> { Message = "Restaurant updated successfully.", Data = result });
        }
        catch (OwnerRestaurantProfileServiceException exception)
        {
            return StatusCode(exception.StatusCode, new ApiErrorResponseDto { Message = exception.Message, Errors = exception.Errors, TraceId = HttpContext.TraceIdentifier });
        }
    }
}
