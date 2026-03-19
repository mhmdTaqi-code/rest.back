using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Auth;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register/user")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<OtpDispatchResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiSuccessResponseDto<OtpDispatchResponseDto>>> RegisterUser(
        [FromBody] RegisterUserRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegisterUserAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiSuccessResponseDto<OtpDispatchResponseDto>
            {
                Message = "User registered successfully. OTP sent to the provided phone number.",
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

    [HttpPost("register/owner")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<OtpDispatchResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiSuccessResponseDto<OtpDispatchResponseDto>>> RegisterOwner(
        [FromBody] RegisterOwnerRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegisterOwnerAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiSuccessResponseDto<OtpDispatchResponseDto>
            {
                Message = "Restaurant owner registered successfully. OTP sent to the owner phone number.",
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

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiSuccessResponseDto<AuthResponseDto>>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            return Ok(new ApiSuccessResponseDto<AuthResponseDto>
            {
                Message = "Login successful.",
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

    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiSuccessResponseDto<AuthResponseDto>>> VerifyOtp(
        [FromBody] VerifyOtpRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.VerifyOtpAsync(request, cancellationToken);
            return Ok(new ApiSuccessResponseDto<AuthResponseDto>
            {
                Message = result.PendingAdminReview
                    ? "OTP verified successfully. Your restaurant request is waiting for admin review."
                    : "OTP verified successfully.",
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
