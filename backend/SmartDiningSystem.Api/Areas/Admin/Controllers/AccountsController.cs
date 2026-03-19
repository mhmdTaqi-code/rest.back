using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SmartDiningSystem.Application.Areas.Admin.Models;
using SmartDiningSystem.Application.Configuration;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = AdminAuthenticationDefaults.CookieScheme, Roles = "Admin")]
[Route("mainadmin/accounts")]
public class AccountsController : Controller
{
    private readonly IAdminAccountService _adminAccountService;

    public AccountsController(IAdminAccountService adminAccountService)
    {
        _adminAccountService = adminAccountService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? role, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Accounts";
        var model = await _adminAccountService.GetAccountsAsync(search, role, cancellationToken);
        return View(model);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Create Account";
        var model = await _adminAccountService.GetCreateModelAsync(cancellationToken);
        return View(model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminAccountFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.RoleOptions = (await _adminAccountService.GetCreateModelAsync(cancellationToken)).RoleOptions;
            ViewData["Title"] = "Create Account";
            return View(model);
        }

        try
        {
            await _adminAccountService.CreateAccountAsync(model, cancellationToken);
            TempData["AdminSuccess"] = "Account created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (AdminAccountServiceException exception)
        {
            ApplyErrorsToModelState(exception, ModelState);
            model.RoleOptions = (await _adminAccountService.GetCreateModelAsync(cancellationToken)).RoleOptions;
            ViewData["Title"] = "Create Account";
            return View(model);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            ViewData["Title"] = "Account Details";
            var model = await _adminAccountService.GetAccountDetailsAsync(id, cancellationToken);
            return View(model);
        }
        catch (AdminAccountServiceException exception) when (exception.IsNotFound)
        {
            return NotFound();
        }
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            ViewData["Title"] = "Edit Account";
            var model = await _adminAccountService.GetEditModelAsync(id, cancellationToken);
            return View(model);
        }
        catch (AdminAccountServiceException exception) when (exception.IsNotFound)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AdminAccountFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.RoleOptions = await LoadRoleOptionsForEditAsync(id, cancellationToken);
            ViewData["Title"] = "Edit Account";
            return View(model);
        }

        try
        {
            await _adminAccountService.UpdateAccountAsync(id, model, GetCurrentAdminUserId(), cancellationToken);
            TempData["AdminSuccess"] = "Account updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminAccountServiceException exception) when (exception.IsNotFound)
        {
            return NotFound();
        }
        catch (AdminAccountServiceException exception)
        {
            ApplyErrorsToModelState(exception, ModelState);
            model.RoleOptions = await LoadRoleOptionsForEditAsync(id, cancellationToken);
            ViewData["Title"] = "Edit Account";
            return View(model);
        }
    }

    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            TempData["AdminSuccess"] = await _adminAccountService.DeleteAccountAsync(
                id,
                GetCurrentAdminUserId(),
                cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (AdminAccountServiceException exception) when (exception.IsNotFound)
        {
            return NotFound();
        }
        catch (AdminAccountServiceException exception)
        {
            TempData["AdminError"] = exception.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost("{id:guid}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _adminAccountService.ActivateAccountAsync(id, GetCurrentAdminUserId(), cancellationToken);
            TempData["AdminSuccess"] = "Account activated successfully.";
        }
        catch (AdminAccountServiceException exception) when (exception.IsNotFound)
        {
            return NotFound();
        }
        catch (AdminAccountServiceException exception)
        {
            TempData["AdminError"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _adminAccountService.DeactivateAccountAsync(id, GetCurrentAdminUserId(), cancellationToken);
            TempData["AdminSuccess"] = "Account deactivated successfully.";
        }
        catch (AdminAccountServiceException exception) when (exception.IsNotFound)
        {
            return NotFound();
        }
        catch (AdminAccountServiceException exception)
        {
            TempData["AdminError"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private Guid? GetCurrentAdminUserId()
    {
        var userId = User.FindFirstValue("userId");
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private async Task<IReadOnlyList<AdminRoleOptionViewModel>> LoadRoleOptionsForEditAsync(Guid id, CancellationToken cancellationToken)
    {
        var editModel = await _adminAccountService.GetEditModelAsync(id, cancellationToken);
        return editModel.RoleOptions;
    }

    private static void ApplyErrorsToModelState(AdminAccountServiceException exception, ModelStateDictionary modelState)
    {
        if (exception.Errors is null || exception.Errors.Count == 0)
        {
            modelState.AddModelError(string.Empty, exception.Message);
            return;
        }

        foreach (var error in exception.Errors)
        {
            foreach (var message in error.Value)
            {
                modelState.AddModelError(error.Key, message);
            }
        }
    }
}
