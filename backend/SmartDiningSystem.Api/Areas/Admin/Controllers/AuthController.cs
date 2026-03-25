using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.Areas.Admin.Models;
using SmartDiningSystem.Application.Configuration;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Areas.Admin.Controllers;

[Area("Admin")]
[AllowAnonymous]
public class AuthController : Controller
{
    private readonly IAdminAuthenticationService _adminAuthenticationService;

    public AuthController(IAdminAuthenticationService adminAuthenticationService)
    {
        _adminAuthenticationService = adminAuthenticationService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        return View(new AdminLoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var principal = await _adminAuthenticationService.AuthenticateAsync(
                model.Username,
                model.Password,
                HttpContext.RequestAborted);
            if (principal is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid admin credentials.");
                return View(model);
            }

            await HttpContext.SignInAsync(AdminAuthenticationDefaults.CookieScheme, principal);

            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }
        catch (AdminAuthenticationConfigurationException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Main Admin credentials are not configured on this server. Contact the system administrator.");
            return View(model);
        }
    }
}
