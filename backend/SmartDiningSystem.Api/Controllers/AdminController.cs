using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Api.Models;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Api.Controllers;

[Route("admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IAdminRestaurantService _adminRestaurantService;

    public AdminController(
        AppDbContext dbContext,
        IPasswordHashService passwordHashService,
        IAdminRestaurantService adminRestaurantService)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
        _adminRestaurantService = adminRestaurantService;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
        {
            return RedirectToAction("Dashboard");
        }

        return View("~/Views/AdminUI/Login.cshtml", new AdminPortalLoginViewModel());
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminPortalLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/AdminUI/Login.cshtml", model);
        }

        try
        {
            var normalizedUsername = model.Username.Trim().ToLowerInvariant();

            var user = await _dbContext.UserAccounts
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    u => u.Username == normalizedUsername && u.IsActive,
                    HttpContext.RequestAborted);

            var isPasswordValid = user != null && _passwordHashService.VerifyPassword(user.PasswordHash, model.Password);

            if (user is null || !isPasswordValid)
            {
                ViewData["ErrorMessage"] = "اسم المستخدم أو كلمة المرور غير صحيحة.";
                return View("~/Views/AdminUI/Login.cshtml", model);
            }

            if (user.Role != UserRole.Admin)
            {
                ViewData["ErrorMessage"] = "هذا الحساب لا يملك صلاحية الدخول إلى لوحة الإدارة.";
                return View("~/Views/AdminUI/Login.cshtml", model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Role, UserRole.Admin.ToString())
            };

            var identity = new ClaimsIdentity(claims, "AdminPortalAuth", ClaimTypes.Name, ClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("AdminPortalAuth", principal);
            
            TempData["SuccessMessage"] = "تم تسجيل الدخول بنجاح.";

            return RedirectToAction("Dashboard");
        }
        catch (Exception)
        {
            ViewData["ErrorMessage"] = "حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.";
            return View("~/Views/AdminUI/Login.cshtml", model);
        }
    }

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = "AdminPortalAuth", Roles = "Admin")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("AdminPortalAuth");
        return RedirectToAction("Login");
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    [Authorize(AuthenticationSchemes = "AdminPortalAuth", Roles = "Admin")]
    public async Task<IActionResult> Dashboard()
    {
        var pendingRestaurants = await _adminRestaurantService.GetPendingRestaurantsAsync(HttpContext.RequestAborted);
        return View("~/Views/AdminUI/Dashboard.cshtml", pendingRestaurants);
    }

    [HttpPost("approve/{id}")]
    [Authorize(AuthenticationSchemes = "AdminPortalAuth", Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            await _adminRestaurantService.ApproveRestaurantAsync(id, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Restaurant approved successfully.";
        }
        catch (AdminRestaurantServiceException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction("Dashboard");
    }

    [HttpPost("reject/{id}")]
    [Authorize(AuthenticationSchemes = "AdminPortalAuth", Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            TempData["ErrorMessage"] = "Rejection reason is required.";
            return RedirectToAction("Dashboard");
        }

        try
        {
            await _adminRestaurantService.RejectRestaurantAsync(id, rejectionReason, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Restaurant rejected successfully.";
        }
        catch (AdminRestaurantServiceException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction("Dashboard");
    }
}
