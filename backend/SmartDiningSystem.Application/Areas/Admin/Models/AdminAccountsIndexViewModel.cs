namespace SmartDiningSystem.Application.Areas.Admin.Models;

public class AdminAccountsIndexViewModel
{
    public string? SearchTerm { get; set; }

    public string? SelectedRole { get; set; }

    public IReadOnlyList<AdminRoleOptionViewModel> RoleOptions { get; set; } = [];

    public IReadOnlyList<AdminAccountListItemViewModel> Accounts { get; set; } = [];
}
