using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.DTOs.Restaurants;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/restaurants")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminRestaurantsController : ControllerBase
{
    private readonly IAdminRestaurantService _adminRestaurantService;

    public AdminRestaurantsController(IAdminRestaurantService adminRestaurantService)
    {
        _adminRestaurantService = adminRestaurantService;
    }

    [HttpGet("pending")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<AdminPendingRestaurantDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<AdminPendingRestaurantDto>>>> GetPendingRestaurants(
        CancellationToken cancellationToken)
    {
        var restaurants = await _adminRestaurantService.GetPendingRestaurantsAsync(cancellationToken);
        return Ok(new ApiSuccessResponseDto<IReadOnlyList<AdminPendingRestaurantDto>>
        {
            Message = "Pending restaurants loaded successfully.",
            Data = restaurants
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<AdminRestaurantDetailsDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<AdminRestaurantDetailsDto>>> CreateRestaurant(
        [FromBody] AdminCreateRestaurantRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var restaurant = await _adminRestaurantService.CreateRestaurantForOwnerAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiSuccessResponseDto<AdminRestaurantDetailsDto>
            {
                Message = "Restaurant created successfully.",
                Data = restaurant
            });
        }
        catch (AdminRestaurantServiceException exception)
        {
            return BuildErrorResponse(exception);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<AdminRestaurantDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<AdminRestaurantDetailsDto>>> GetRestaurantDetails(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var restaurant = await _adminRestaurantService.GetRestaurantDetailsAsync(id, cancellationToken);
            return Ok(new ApiSuccessResponseDto<AdminRestaurantDetailsDto>
            {
                Message = "Restaurant request loaded successfully.",
                Data = restaurant
            });
        }
        catch (AdminRestaurantServiceException exception)
        {
            return BuildErrorResponse(exception);
        }
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<AdminRestaurantDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<AdminRestaurantDetailsDto>>> ApproveRestaurant(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var restaurant = await _adminRestaurantService.ApproveRestaurantAsync(id, cancellationToken);
            return Ok(new ApiSuccessResponseDto<AdminRestaurantDetailsDto>
            {
                Message = "Restaurant approved successfully.",
                Data = restaurant
            });
        }
        catch (AdminRestaurantServiceException exception)
        {
            return BuildErrorResponse(exception);
        }
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<AdminRestaurantDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<AdminRestaurantDetailsDto>>> RejectRestaurant(
        Guid id,
        [FromBody] AdminRejectRestaurantRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var restaurant = await _adminRestaurantService.RejectRestaurantAsync(id, request.RejectionReason, cancellationToken);
            return Ok(new ApiSuccessResponseDto<AdminRestaurantDetailsDto>
            {
                Message = "Restaurant rejected successfully.",
                Data = restaurant
            });
        }
        catch (AdminRestaurantServiceException exception)
        {
            return BuildErrorResponse(exception);
        }
    }

    private ActionResult<ApiSuccessResponseDto<AdminRestaurantDetailsDto>> BuildErrorResponse(AdminRestaurantServiceException exception)
    {
        return StatusCode(exception.StatusCode, new ApiErrorResponseDto
        {
            Message = exception.Message,
            Errors = exception.Errors,
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
