using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.Areas.Admin.Models;
using SmartDiningSystem.Infrastructure.Data;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Infrastructure.Services;

public class AdminAccountService : IAdminAccountService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;

    public AdminAccountService(AppDbContext dbContext, IPasswordHashService passwordHashService)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
    }

    public async Task<AdminAccountsIndexViewModel> GetAccountsAsync(
        string? searchTerm,
        string? role,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.UserAccounts
            .AsNoTracking()
            .VisibleToAdminUi()
            .Select(account => new
            {
                Account = account,
                LatestRestaurantApprovalStatus = account.OwnedRestaurants
                    .OrderBy(restaurant => restaurant.CreatedAtUtc)
                    .Select(restaurant => (string?)restaurant.ApprovalStatus.ToString())
                    .FirstOrDefault()
            })
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(row =>
                EF.Functions.ILike(row.Account.FullName, $"%{term}%") ||
                EF.Functions.ILike(row.Account.PhoneNumber, $"%{term}%"));
        }

        UserRole? roleFilter = null;
        if (!string.IsNullOrWhiteSpace(role) && TryParseVisibleRole(role, out var parsedRole))
        {
            roleFilter = parsedRole;
            query = query.Where(row => row.Account.Role == parsedRole);
        }

        var accounts = await query
            .OrderBy(row => row.Account.FullName)
            .Select(row => new AdminAccountListItemViewModel
            {
                Id = row.Account.Id,
                FullName = row.Account.FullName,
                Username = row.Account.Username,
                PhoneNumber = row.Account.PhoneNumber,
                Email = row.Account.Email,
                Role = row.Account.Role.ToString(),
                IsActive = row.Account.IsActive,
                IsPhoneVerified = row.Account.IsPhoneVerified,
                RestaurantApprovalStatus = row.LatestRestaurantApprovalStatus,
                CreatedAtUtc = row.Account.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new AdminAccountsIndexViewModel
        {
            SearchTerm = searchTerm?.Trim(),
            SelectedRole = roleFilter?.ToString(),
            RoleOptions = BuildRoleOptions(roleFilter?.ToString()),
            Accounts = accounts
        };
    }

    public async Task<AdminAccountDetailsViewModel> GetAccountDetailsAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await _dbContext.UserAccounts
            .AsNoTracking()
            .VisibleToAdminUi()
            .Where(user => user.Id == accountId)
            .Select(user => new
            {
                Account = user,
                LinkedRestaurant = user.OwnedRestaurants
                    .OrderBy(restaurant => restaurant.CreatedAtUtc)
                    .Select(restaurant => new
                    {
                        restaurant.Name,
                        ApprovalStatus = restaurant.ApprovalStatus.ToString(),
                        restaurant.RejectionReason
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
        {
            throw new AdminAccountServiceException("Account not found.", isNotFound: true);
        }

        return new AdminAccountDetailsViewModel
        {
            Id = account.Account.Id,
            FullName = account.Account.FullName,
            Username = account.Account.Username,
            PhoneNumber = account.Account.PhoneNumber,
            Email = account.Account.Email,
            Role = account.Account.Role.ToString(),
            IsActive = account.Account.IsActive,
            IsPhoneVerified = account.Account.IsPhoneVerified,
            RestaurantName = account.LinkedRestaurant?.Name,
            RestaurantApprovalStatus = account.LinkedRestaurant?.ApprovalStatus,
            RestaurantRejectionReason = account.LinkedRestaurant?.RejectionReason,
            CreatedAtUtc = account.Account.CreatedAtUtc
        };
    }

    public Task<AdminAccountFormViewModel> GetCreateModelAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new AdminAccountFormViewModel
        {
            IsActive = true,
            IsPhoneVerified = true,
            Username = string.Empty,
            Role = UserRole.User.ToString(),
            RoleOptions = BuildRoleOptions(UserRole.User.ToString()),
            RestaurantDescription = string.Empty,
            RestaurantPhoneNumber = string.Empty
        });
    }

    public async Task<AdminAccountFormViewModel> GetEditModelAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await _dbContext.UserAccounts
            .AsNoTracking()
            .VisibleToAdminUi()
            .FirstOrDefaultAsync(user => user.Id == accountId, cancellationToken);

        if (account is null)
        {
            throw new AdminAccountServiceException("Account not found.", isNotFound: true);
        }

        Restaurant? linkedRestaurant = null;
        if (account.Role == UserRole.RestaurantOwner)
        {
            linkedRestaurant = await _dbContext.Restaurants
                .AsNoTracking()
                .OrderBy(restaurant => restaurant.CreatedAtUtc)
                .FirstOrDefaultAsync(restaurant => restaurant.OwnerId == account.Id, cancellationToken);
        }

        return new AdminAccountFormViewModel
        {
            Id = account.Id,
            FullName = account.FullName,
            Username = account.Username,
            PhoneNumber = account.PhoneNumber ?? string.Empty,
            Email = account.Email,
            Role = account.Role.ToString(),
            IsActive = account.IsActive,
            IsPhoneVerified = account.IsPhoneVerified,
            RoleOptions = BuildRoleOptions(account.Role.ToString()),
            RestaurantName = linkedRestaurant?.Name,
            RestaurantDescription = linkedRestaurant?.Description ?? string.Empty,
            RestaurantAddress = linkedRestaurant?.Address,
            RestaurantPhoneNumber = account.PhoneNumber
        };
    }

    public async Task CreateAccountAsync(AdminAccountFormViewModel model, CancellationToken cancellationToken)
    {
        await ValidateModelAsync(model, null, cancellationToken);

        var selectedRole = ParseVisibleRole(model.Role);
        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            FullName = model.FullName.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(model.Email)
                ? $"admin-managed-{Guid.NewGuid():N}@local.invalid"
                : model.Email.Trim(),
            Username = NormalizeUsername(model.Username),
            PasswordHash = _passwordHashService.HashPassword(model.Password),
            Role = selectedRole,
            IsActive = model.IsActive,
            IsPhoneVerified = model.IsPhoneVerified,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _dbContext.UserAccounts.Add(account);

        if (selectedRole == UserRole.RestaurantOwner)
        {
            _dbContext.Restaurants.Add(BuildRestaurant(account.Id, model));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAccountAsync(
        Guid accountId,
        AdminAccountFormViewModel model,
        Guid? currentAdminUserId,
        CancellationToken cancellationToken)
    {
        var account = await _dbContext.UserAccounts
            .VisibleToAdminUi()
            .FirstOrDefaultAsync(user => user.Id == accountId, cancellationToken);

        if (account is null)
        {
            throw new AdminAccountServiceException("Account not found.", isNotFound: true);
        }

        await ValidateModelAsync(model, accountId, cancellationToken);
        await EnforceAdminSafetyRulesAsync(account, model, currentAdminUserId, cancellationToken);

        var selectedRole = ParseVisibleRole(model.Role);
        account.FullName = model.FullName.Trim();
        account.PhoneNumber = model.PhoneNumber.Trim();
        account.Email = string.IsNullOrWhiteSpace(model.Email)
            ? account.Email
            : model.Email.Trim();
        account.Username = NormalizeUsername(model.Username);
        account.Role = selectedRole;
        account.IsActive = model.IsActive;
        account.IsPhoneVerified = model.IsPhoneVerified;
        account.UpdatedAtUtc = DateTime.UtcNow;

        if (selectedRole == UserRole.RestaurantOwner)
        {
            var restaurant = await _dbContext.Restaurants
                .OrderBy(existingRestaurant => existingRestaurant.CreatedAtUtc)
                .FirstOrDefaultAsync(existingRestaurant => existingRestaurant.OwnerId == account.Id, cancellationToken);

            if (restaurant is null)
            {
                _dbContext.Restaurants.Add(BuildRestaurant(account.Id, model));
            }
            else
            {
                UpdateRestaurant(restaurant, model);
            }
        }
        else
        {
            var hasLinkedRestaurant = await _dbContext.Restaurants
                .AnyAsync(existingRestaurant => existingRestaurant.OwnerId == account.Id, cancellationToken);

            if (hasLinkedRestaurant)
            {
                throw new AdminAccountServiceException(
                    "This account has linked restaurant data. Keep it as RestaurantOwner for now.");
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> DeleteAccountAsync(Guid accountId, Guid? currentAdminUserId, CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(accountId, cancellationToken);

        if (currentAdminUserId.HasValue && currentAdminUserId.Value == account.Id)
        {
            throw new AdminAccountServiceException("You cannot delete the currently logged-in admin account.");
        }

        if (account.Role == UserRole.Admin)
        {
            var adminCount = await _dbContext.UserAccounts
                .VisibleToAdminUi()
                .CountAsync(user => user.Role == UserRole.Admin, cancellationToken);

            if (adminCount <= 1)
            {
                throw new AdminAccountServiceException("You cannot remove the last admin account.");
            }
        }

        if (account.Role == UserRole.Admin && account.IsActive)
        {
            var activeAdmins = await _dbContext.UserAccounts
                .VisibleToAdminUi()
                .CountAsync(user => user.Role == UserRole.Admin && user.IsActive, cancellationToken);

            if (activeAdmins <= 1)
            {
                throw new AdminAccountServiceException(
                    "This account cannot be deleted because it is the last active admin account.");
            }
        }

        var ownedRestaurants = await _dbContext.Restaurants
            .Where(restaurant => restaurant.OwnerId == account.Id)
            .OrderBy(restaurant => restaurant.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var hasUserOrderHistory = await _dbContext.Orders
            .AnyAsync(order => order.UserId == account.Id, cancellationToken);

        if (hasUserOrderHistory)
        {
            throw new AdminAccountServiceException(
                "This account cannot be hard-deleted because it is referenced by existing orders.");
        }

        foreach (var restaurant in ownedRestaurants)
        {
            var hasRestaurantOrders = await _dbContext.Orders
                .AnyAsync(order => order.RestaurantId == restaurant.Id, cancellationToken);

            if (hasRestaurantOrders)
            {
                throw new AdminAccountServiceException(
                    "This restaurant owner cannot be hard-deleted because the linked restaurant has order history.");
            }

            var hasMenuItemOrderHistory = await _dbContext.OrderItems
                .AnyAsync(orderItem => _dbContext.MenuItems
                    .Where(menuItem => menuItem.Id == orderItem.MenuItemId)
                    .Any(menuItem => menuItem.RestaurantId == restaurant.Id), cancellationToken);

            if (hasMenuItemOrderHistory)
            {
                throw new AdminAccountServiceException(
                    "This restaurant owner cannot be hard-deleted because linked menu items are referenced by order history.");
            }

            var hasTableOrderHistory = await _dbContext.Orders
                .AnyAsync(order => _dbContext.RestaurantTables
                    .Where(table => table.Id == order.RestaurantTableId)
                    .Any(table => table.RestaurantId == restaurant.Id), cancellationToken);

            if (hasTableOrderHistory)
            {
                throw new AdminAccountServiceException(
                    "This restaurant owner cannot be hard-deleted because linked restaurant tables are referenced by order history.");
            }
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (ownedRestaurants.Count > 0)
            {
                var restaurantIds = ownedRestaurants.Select(restaurant => restaurant.Id).ToList();

                var restaurantMenuItems = await _dbContext.MenuItems
                    .Where(menuItem => restaurantIds.Contains(menuItem.RestaurantId))
                    .ToListAsync(cancellationToken);

                var restaurantTables = await _dbContext.RestaurantTables
                    .Where(table => restaurantIds.Contains(table.RestaurantId))
                    .ToListAsync(cancellationToken);

                if (restaurantMenuItems.Count > 0)
                {
                    _dbContext.MenuItems.RemoveRange(restaurantMenuItems);
                }

                if (restaurantTables.Count > 0)
                {
                    _dbContext.RestaurantTables.RemoveRange(restaurantTables);
                }

                _dbContext.Restaurants.RemoveRange(ownedRestaurants);
            }

            var otpCodes = await _dbContext.OtpCodes
                .Where(otp => otp.UserAccountId == account.Id)
                .ToListAsync(cancellationToken);

            if (otpCodes.Count > 0)
            {
                _dbContext.OtpCodes.RemoveRange(otpCodes);
            }

            _dbContext.UserAccounts.Remove(account);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return "Account permanently deleted.";
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ActivateAccountAsync(Guid accountId, Guid? currentAdminUserId, CancellationToken cancellationToken)
    {
        _ = currentAdminUserId;
        var account = await FindAccountAsync(accountId, cancellationToken);

        if (account.IsActive)
        {
            return;
        }

        account.IsActive = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAccountAsync(Guid accountId, Guid? currentAdminUserId, CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(accountId, cancellationToken);

        if (!account.IsActive)
        {
            return;
        }

        if (currentAdminUserId.HasValue && currentAdminUserId.Value == account.Id)
        {
            throw new AdminAccountServiceException("You cannot deactivate the currently signed-in admin account.");
        }

        if (account.Role == UserRole.Admin)
        {
            var activeAdmins = await _dbContext.UserAccounts
                .CountAsync(user => user.Role == UserRole.Admin && user.IsActive, cancellationToken);

            if (activeAdmins <= 1)
            {
                throw new AdminAccountServiceException("You cannot deactivate the last active admin account.");
            }
        }

        account.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserAccount> FindAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await _dbContext.UserAccounts
            .VisibleToAdminUi()
            .FirstOrDefaultAsync(user => user.Id == accountId, cancellationToken);

        if (account is null)
        {
            throw new AdminAccountServiceException("Account not found.", isNotFound: true);
        }

        return account;
    }

    private async Task ValidateModelAsync(
        AdminAccountFormViewModel model,
        Guid? currentAccountId,
        CancellationToken cancellationToken)
    {
        if (!TryParseVisibleRole(model.Role, out _))
        {
            throw new AdminAccountServiceException(
                "Validation failed.",
                errors: new Dictionary<string, string[]>
                {
                    [nameof(model.Role)] = ["Please select a valid role."]
                });
        }

        var errors = new Dictionary<string, string[]>();
        var normalizedPhone = model.PhoneNumber.Trim();
        var normalizedEmail = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        var normalizedUsername = NormalizeUsername(model.Username);
        var normalizedRestaurantName = string.IsNullOrWhiteSpace(model.RestaurantName) ? null : model.RestaurantName.Trim();
        var normalizedRestaurantDescription = string.IsNullOrWhiteSpace(model.RestaurantDescription) ? null : model.RestaurantDescription.Trim();
        var normalizedRestaurantAddress = string.IsNullOrWhiteSpace(model.RestaurantAddress) ? null : model.RestaurantAddress.Trim();

        if (!currentAccountId.HasValue)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                errors[nameof(model.Password)] = ["Password is required when creating an account."];
            }
            else if (model.Password.Length < 6)
            {
                errors[nameof(model.Password)] = ["Password must be at least 6 characters long."];
            }

            if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                errors[nameof(model.ConfirmPassword)] = ["Please confirm the password."];
            }
            else if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
            {
                errors[nameof(model.ConfirmPassword)] = ["Confirm password must match the password."];
            }
        }

        var phoneExists = await _dbContext.UserAccounts
            .AnyAsync(
                user => user.PhoneNumber == normalizedPhone && user.Id != currentAccountId,
                cancellationToken);

        if (phoneExists)
        {
            errors[nameof(model.PhoneNumber)] = ["Another account already uses this phone number."];
        }

        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            var emailExists = await _dbContext.UserAccounts
                .AnyAsync(
                    user => user.Email == normalizedEmail && user.Id != currentAccountId,
                    cancellationToken);

            if (emailExists)
            {
                errors[nameof(model.Email)] = ["Another account already uses this email address."];
            }
        }

        var usernameExists = await _dbContext.UserAccounts
            .AnyAsync(
                user => user.Username == normalizedUsername && user.Id != currentAccountId,
                cancellationToken);

        if (usernameExists)
        {
            errors[nameof(model.Username)] = ["Another account already uses this username."];
        }

        if (TryParseVisibleRole(model.Role, out var selectedRole) && selectedRole == UserRole.RestaurantOwner)
        {
            if (string.IsNullOrWhiteSpace(normalizedRestaurantName))
            {
                errors[nameof(model.RestaurantName)] = ["Restaurant name is required for restaurant owners."];
            }

            if (string.IsNullOrWhiteSpace(normalizedRestaurantDescription))
            {
                errors[nameof(model.RestaurantDescription)] = ["Restaurant description is required for restaurant owners."];
            }

            if (string.IsNullOrWhiteSpace(normalizedRestaurantAddress))
            {
                errors[nameof(model.RestaurantAddress)] = ["Restaurant address is required for restaurant owners."];
            }
        }

        if (errors.Count > 0)
        {
            throw new AdminAccountServiceException("Validation failed.", errors: errors);
        }
    }

    private async Task EnforceAdminSafetyRulesAsync(
        UserAccount existingAccount,
        AdminAccountFormViewModel model,
        Guid? currentAdminUserId,
        CancellationToken cancellationToken)
    {
        if (!TryParseVisibleRole(model.Role, out var newRole))
        {
            return;
        }

        var isDemotingAdmin = existingAccount.Role == UserRole.Admin && newRole != UserRole.Admin;
        var isDeactivatingAdmin = existingAccount.Role == UserRole.Admin && existingAccount.IsActive && !model.IsActive;

        if (currentAdminUserId.HasValue &&
            currentAdminUserId.Value == existingAccount.Id &&
            (isDemotingAdmin || isDeactivatingAdmin))
        {
            throw new AdminAccountServiceException("You cannot remove admin access from the currently signed-in admin account.");
        }

        if (!isDemotingAdmin && !isDeactivatingAdmin)
        {
            return;
        }

        var activeAdmins = await _dbContext.UserAccounts
            .VisibleToAdminUi()
            .CountAsync(user => user.Role == UserRole.Admin && user.IsActive, cancellationToken);

        if (activeAdmins <= 1)
        {
            throw new AdminAccountServiceException("You cannot remove or deactivate the last active admin account.");
        }
    }

    private static IReadOnlyList<AdminRoleOptionViewModel> BuildRoleOptions(string? selectedRole)
    {
        return GetVisibleRoles()
            .Select(role => new AdminRoleOptionViewModel
            {
                Value = role.ToString(),
                Label = role.ToString(),
                Selected = string.Equals(role.ToString(), selectedRole, StringComparison.Ordinal)
            })
            .ToList();
    }

    private static IReadOnlyList<UserRole> GetVisibleRoles()
    {
        return
        [
            UserRole.User,
            UserRole.RestaurantOwner,
            UserRole.Admin
        ];
    }

    private static bool TryParseVisibleRole(string? value, out UserRole role)
    {
        if (!Enum.TryParse<UserRole>(value, out role))
        {
            return false;
        }

        return GetVisibleRoles().Contains(role);
    }

    private static UserRole ParseVisibleRole(string? value)
    {
        if (TryParseVisibleRole(value, out var role))
        {
            return role;
        }

        throw new AdminAccountServiceException(
            "Validation failed.",
            errors: new Dictionary<string, string[]>
            {
                [nameof(AdminAccountFormViewModel.Role)] = ["Please select a valid role."]
            });
    }

    private static Restaurant BuildRestaurant(Guid ownerId, AdminAccountFormViewModel model)
    {
        return new Restaurant
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = model.RestaurantName!.Trim(),
            Description = model.RestaurantDescription!.Trim(),
            Address = model.RestaurantAddress!.Trim(),
            ContactPhone = model.PhoneNumber.Trim(),
            ApprovalStatus = RestaurantApprovalStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static void UpdateRestaurant(Restaurant restaurant, AdminAccountFormViewModel model)
    {
        restaurant.Name = model.RestaurantName!.Trim();
        restaurant.Description = model.RestaurantDescription!.Trim();
        restaurant.Address = model.RestaurantAddress!.Trim();
        restaurant.ContactPhone = model.PhoneNumber.Trim();
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().ToLowerInvariant();
    }
}
