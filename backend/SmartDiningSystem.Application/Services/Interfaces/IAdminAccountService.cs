using SmartDiningSystem.Application.DTOs.Accounts;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IAdminAccountService
{
    Task<IReadOnlyList<AdminAccountListItemDto>> GetAccountsAsync(
        string? searchTerm,
        string? role,
        CancellationToken cancellationToken);

    Task<AccountMutationResultDto> CreateAccountAsync(
        SaveAdminAccountRequestDto request,
        CancellationToken cancellationToken);

    Task<AccountMutationResultDto> UpdateAccountAsync(
        Guid accountId,
        SaveAdminAccountRequestDto request,
        Guid? currentAdminUserId,
        CancellationToken cancellationToken);

    Task<string> DeleteAccountAsync(Guid accountId, Guid? currentAdminUserId, CancellationToken cancellationToken);
}
