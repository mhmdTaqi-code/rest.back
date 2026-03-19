using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.Configuration;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = AdminAuthenticationDefaults.CookieScheme, Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IAdminDashboardService _adminDashboardService;

    public DashboardController(IAdminDashboardService adminDashboardService)
    {
        _adminDashboardService = adminDashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _adminDashboardService.GetDashboardAsync(cancellationToken);
        return View(model);
    }
}
