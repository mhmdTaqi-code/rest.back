using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.DTOs.RestaurantTables;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/owner/restaurants/{restaurantId:guid}/tables")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "RestaurantOwner")]
public class OwnerRestaurantTablesController : ControllerBase
{
    private readonly IRestaurantTableManagementService _restaurantTableManagementService;
    public OwnerRestaurantTablesController(IRestaurantTableManagementService restaurantTableManagementService) => _restaurantTableManagementService = restaurantTableManagementService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<RestaurantTableDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<RestaurantTableDto>>>> GetTables(Guid restaurantId, CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId(); if (ownerId is null) return Unauthorized(BuildUnauthorizedResponse());
        try { var tables = await _restaurantTableManagementService.GetOwnerTablesAsync(ownerId.Value, restaurantId, cancellationToken); return Ok(new ApiSuccessResponseDto<IReadOnlyList<RestaurantTableDto>> { Message = "Restaurant tables loaded successfully.", Data = tables }); }
        catch (RestaurantTableManagementServiceException exception) { return BuildErrorResponse<IReadOnlyList<RestaurantTableDto>>(exception); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<RestaurantTableDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiSuccessResponseDto<RestaurantTableDto>>> CreateTable(Guid restaurantId, [FromBody] CreateRestaurantTableRequestDto request, CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId(); if (ownerId is null) return Unauthorized(BuildUnauthorizedResponse());
        try { var table = await _restaurantTableManagementService.CreateTableAsync(ownerId.Value, restaurantId, request, cancellationToken); return StatusCode(StatusCodes.Status201Created, new ApiSuccessResponseDto<RestaurantTableDto> { Message = "Restaurant table created successfully.", Data = table }); }
        catch (RestaurantTableManagementServiceException exception) { return BuildErrorResponse<RestaurantTableDto>(exception); }
    }

    [HttpPost("bulk")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<RestaurantTableDto>>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<RestaurantTableDto>>>> BulkCreateTables(Guid restaurantId, [FromBody] BulkCreateRestaurantTablesRequestDto request, CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId(); if (ownerId is null) return Unauthorized(BuildUnauthorizedResponse());
        try { var tables = await _restaurantTableManagementService.BulkCreateTablesAsync(ownerId.Value, restaurantId, request, cancellationToken); return StatusCode(StatusCodes.Status201Created, new ApiSuccessResponseDto<IReadOnlyList<RestaurantTableDto>> { Message = "Restaurant tables created successfully.", Data = tables }); }
        catch (RestaurantTableManagementServiceException exception) { return BuildErrorResponse<IReadOnlyList<RestaurantTableDto>>(exception); }
    }

    [HttpPatch("{tableId:guid}/status")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<RestaurantTableDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<RestaurantTableDto>>> UpdateTableStatus(Guid restaurantId, Guid tableId, [FromBody] UpdateRestaurantTableStatusRequestDto request, CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId(); if (ownerId is null) return Unauthorized(BuildUnauthorizedResponse());
        try { var table = await _restaurantTableManagementService.UpdateTableStatusAsync(ownerId.Value, restaurantId, tableId, request, cancellationToken); return Ok(new ApiSuccessResponseDto<RestaurantTableDto> { Message = "Restaurant table updated successfully.", Data = table }); }
        catch (RestaurantTableManagementServiceException exception) { return BuildErrorResponse<RestaurantTableDto>(exception); }
    }

    [HttpDelete("{tableId:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<object>>> DeleteTable(Guid restaurantId, Guid tableId, CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId(); if (ownerId is null) return Unauthorized(BuildUnauthorizedResponse());
        try { await _restaurantTableManagementService.DeleteTableAsync(ownerId.Value, restaurantId, tableId, cancellationToken); return Ok(new ApiSuccessResponseDto<object> { Message = "Restaurant table deleted successfully." }); }
        catch (RestaurantTableManagementServiceException exception) { return BuildErrorResponse<object>(exception); }
    }

    private Guid? GetOwnerId() => Guid.TryParse(User.FindFirstValue("userId"), out var parsed) ? parsed : null;
    private ApiErrorResponseDto BuildUnauthorizedResponse() => new() { Message = "Unauthorized owner context.", TraceId = HttpContext.TraceIdentifier };
    private ActionResult<ApiSuccessResponseDto<T>> BuildErrorResponse<T>(RestaurantTableManagementServiceException exception) => StatusCode(exception.StatusCode, new ApiErrorResponseDto { Message = exception.Message, Errors = exception.Errors, TraceId = HttpContext.TraceIdentifier });
}
