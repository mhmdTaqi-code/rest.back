using SmartDiningSystem.Application.Areas.Admin.Models;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IAdminAccountService
{
    Task<AdminAccountsIndexViewModel> GetAccountsAsync(
        string? searchTerm,
        string? role,
        CancellationToken cancellationToken);

    Task<AdminAccountDetailsViewModel> GetAccountDetailsAsync(Guid accountId, CancellationToken cancellationToken);

    Task<AdminAccountFormViewModel> GetCreateModelAsync(CancellationToken cancellationToken);

    Task<AdminAccountFormViewModel> GetEditModelAsync(Guid accountId, CancellationToken cancellationToken);

    Task CreateAccountAsync(AdminAccountFormViewModel model, CancellationToken cancellationToken);

    Task UpdateAccountAsync(
        Guid accountId,
        AdminAccountFormViewModel model,
        Guid? currentAdminUserId,
        CancellationToken cancellationToken);

    Task<string> DeleteAccountAsync(Guid accountId, Guid? currentAdminUserId, CancellationToken cancellationToken);

    Task ActivateAccountAsync(Guid accountId, Guid? currentAdminUserId, CancellationToken cancellationToken);

    Task DeactivateAccountAsync(Guid accountId, Guid? currentAdminUserId, CancellationToken cancellationToken);
}
