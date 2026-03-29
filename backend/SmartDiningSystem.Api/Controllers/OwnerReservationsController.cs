using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.DTOs.Reservations;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/owner/reservations")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "RestaurantOwner")]
public class OwnerReservationsController : ControllerBase
{
    private readonly ITableReservationService _tableReservationService;

    public OwnerReservationsController(ITableReservationService tableReservationService)
    {
        _tableReservationService = tableReservationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<OwnerReservationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<OwnerReservationDto>>>> GetReservations(
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var reservations = await _tableReservationService.GetOwnerReservationsAsync(ownerId.Value, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<OwnerReservationDto>>
            {
                Message = "Owner reservations loaded successfully.",
                Data = reservations
            });
        }
        catch (TableReservationServiceException exception)
        {
            return BuildErrorResponse<IReadOnlyList<OwnerReservationDto>>(exception);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<OwnerReservationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<OwnerReservationDto>>> GetReservation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var reservation = await _tableReservationService.GetOwnerReservationAsync(ownerId.Value, id, cancellationToken);
            return Ok(new ApiSuccessResponseDto<OwnerReservationDto>
            {
                Message = "Owner reservation loaded successfully.",
                Data = reservation
            });
        }
        catch (TableReservationServiceException exception)
        {
            return BuildErrorResponse<OwnerReservationDto>(exception);
        }
    }

    [HttpPost("{id:guid}/check-in")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<OwnerReservationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<OwnerReservationDto>>> CheckInReservation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        if (ownerId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var reservation = await _tableReservationService.CheckInReservationAsync(ownerId.Value, id, cancellationToken);
            return Ok(new ApiSuccessResponseDto<OwnerReservationDto>
            {
                Message = "Reservation checked in successfully.",
                Data = reservation
            });
        }
        catch (TableReservationServiceException exception)
        {
            return BuildErrorResponse<OwnerReservationDto>(exception);
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

    private ActionResult<ApiSuccessResponseDto<T>> BuildErrorResponse<T>(TableReservationServiceException exception)
    {
        return StatusCode(exception.StatusCode, new ApiErrorResponseDto
        {
            Message = exception.Message,
            Errors = exception.Errors,
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
