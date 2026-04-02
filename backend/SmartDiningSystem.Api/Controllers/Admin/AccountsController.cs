using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.Configuration;
using SmartDiningSystem.Application.DTOs.Accounts;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers.Admin;

[ApiController]
[Route("api/accounts")]
[Authorize(AuthenticationSchemes = AdminAuthenticationDefaults.CookieScheme + "," + JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AccountsController : ControllerBase
{
    private readonly IAdminAccountService _adminAccountService;

    public AccountsController(IAdminAccountService adminAccountService)
    {
        _adminAccountService = adminAccountService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<AdminAccountListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<AdminAccountListItemDto>>>> GetAccounts(
        [FromQuery] string? search,
        [FromQuery] string? role,
        CancellationToken cancellationToken)
    {
        var accounts = await _adminAccountService.GetAccountsAsync(search, role, cancellationToken);

        return Ok(new ApiSuccessResponseDto<IReadOnlyList<AdminAccountListItemDto>>
        {
            Message = "Accounts loaded successfully.",
            Data = accounts
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<AccountMutationResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiSuccessResponseDto<AccountMutationResultDto>>> CreateAccount(
        [FromBody] SaveAdminAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _adminAccountService.CreateAccountAsync(request, cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new ApiSuccessResponseDto<AccountMutationResultDto>
            {
                Message = "Account created successfully.",
                Data = result
            });
        }
        catch (AdminAccountServiceException exception)
        {
            return BuildErrorResponse(exception);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<AccountMutationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<AccountMutationResultDto>>> UpdateAccount(
        Guid id,
        [FromBody] SaveAdminAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _adminAccountService.UpdateAccountAsync(
                id,
                request,
                GetCurrentAdminUserId(),
                cancellationToken);

            return Ok(new ApiSuccessResponseDto<AccountMutationResultDto>
            {
                Message = "Account updated successfully.",
                Data = result
            });
        }
        catch (AdminAccountServiceException exception)
        {
            return BuildErrorResponse(exception);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<AccountMutationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponseDto<AccountMutationResultDto>>> DeleteAccount(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _adminAccountService.DeleteAccountAsync(id, GetCurrentAdminUserId(), cancellationToken);

            return Ok(new ApiSuccessResponseDto<AccountMutationResultDto>
            {
                Message = message,
                Data = new AccountMutationResultDto
                {
                    Id = id
                }
            });
        }
        catch (AdminAccountServiceException exception)
        {
            return BuildErrorResponse(exception);
        }
    }

    private Guid? GetCurrentAdminUserId()
    {
        var userId = User.FindFirstValue("userId");
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private ActionResult BuildErrorResponse(AdminAccountServiceException exception)
    {
        var statusCode = exception.IsNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;

        return StatusCode(statusCode, new ApiErrorResponseDto
        {
            Message = exception.Message,
            Errors = exception.Errors ?? new Dictionary<string, string[]>(),
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
