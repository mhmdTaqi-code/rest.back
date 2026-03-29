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
[Route("api/reservations")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ReservationsController : ControllerBase
{
    private readonly ITableReservationService _tableReservationService;

    public ReservationsController(ITableReservationService tableReservationService)
    {
        _tableReservationService = tableReservationService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<ReservationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiSuccessResponseDto<ReservationDto>>> CreateReservation(
        [FromBody] CreateReservationRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var reservation = await _tableReservationService.CreateReservationAsync(userId.Value, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiSuccessResponseDto<ReservationDto>
            {
                Message = "Reservation created successfully.",
                Data = reservation
            });
        }
        catch (TableReservationServiceException exception)
        {
            return BuildErrorResponse<ReservationDto>(exception);
        }
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<ReservationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<ReservationDto>>>> GetMyReservations(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var reservations = await _tableReservationService.GetMyReservationsAsync(userId.Value, cancellationToken);
            return Ok(new ApiSuccessResponseDto<IReadOnlyList<ReservationDto>>
            {
                Message = "Reservations loaded successfully.",
                Data = reservations
            });
        }
        catch (TableReservationServiceException exception)
        {
            return BuildErrorResponse<IReadOnlyList<ReservationDto>>(exception);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<ReservationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<ReservationDto>>> GetReservation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var reservation = await _tableReservationService.GetMyReservationAsync(userId.Value, id, cancellationToken);
            return Ok(new ApiSuccessResponseDto<ReservationDto>
            {
                Message = "Reservation loaded successfully.",
                Data = reservation
            });
        }
        catch (TableReservationServiceException exception)
        {
            return BuildErrorResponse<ReservationDto>(exception);
        }
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<ReservationDto>>> CancelReservation(
        Guid id,
        [FromBody] CancelReservationRequestDto? request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var reservation = await _tableReservationService.CancelReservationAsync(userId.Value, id, request, cancellationToken);
            return Ok(new ApiSuccessResponseDto<ReservationDto>
            {
                Message = "Reservation cancelled successfully.",
                Data = reservation
            });
        }
        catch (TableReservationServiceException exception)
        {
            return BuildErrorResponse<ReservationDto>(exception);
        }
    }

    [HttpPost("{id:guid}/mark-deposit-paid")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<ReservationDto>>> MarkDepositPaid(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized(BuildUnauthorizedResponse());
        }

        try
        {
            var reservation = await _tableReservationService.MarkDepositPaidAsync(userId.Value, id, cancellationToken);
            return Ok(new ApiSuccessResponseDto<ReservationDto>
            {
                Message = "Reservation deposit marked as paid successfully.",
                Data = reservation
            });
        }
        catch (TableReservationServiceException exception)
        {
            return BuildErrorResponse<ReservationDto>(exception);
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
