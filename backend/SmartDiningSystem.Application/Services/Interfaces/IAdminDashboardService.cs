using SmartDiningSystem.Application.Areas.Admin.Models;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface IAdminDashboardService
{
    Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken);
}
