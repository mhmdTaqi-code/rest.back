using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.Accounts;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

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

    public async Task<IReadOnlyList<AdminAccountListItemDto>> GetAccountsAsync(
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

        if (!string.IsNullOrWhiteSpace(role) && TryParseVisibleRole(role, out var parsedRole))
        {
            query = query.Where(row => row.Account.Role == parsedRole);
        }

        return await query
            .OrderBy(row => row.Account.FullName)
            .Select(row => new AdminAccountListItemDto
            {
                Id = row.Account.Id,
                FullName = row.Account.FullName,
                Username = row.Account.Username,
                PhoneNumber = row.Account.PhoneNumber,
                Role = row.Account.Role.ToString(),
                IsActive = row.Account.IsActive,
                IsPhoneVerified = row.Account.IsPhoneVerified,
                RestaurantApprovalStatus = row.LatestRestaurantApprovalStatus,
                CreatedAtUtc = row.Account.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountMutationResultDto> CreateAccountAsync(
        SaveAdminAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        var effectiveRequest = ApplyCreateDefaults(request);
        await ValidateRequestAsync(effectiveRequest, null, cancellationToken);

        var selectedRole = ParseVisibleRole(effectiveRequest.Role);
        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            FullName = effectiveRequest.FullName!.Trim(),
            PhoneNumber = effectiveRequest.PhoneNumber!.Trim(),
            Username = NormalizeUsername(effectiveRequest.Username!),
            PasswordHash = _passwordHashService.HashPassword(effectiveRequest.Password!),
            Role = selectedRole,
            IsActive = effectiveRequest.IsActive ?? true,
            IsPhoneVerified = effectiveRequest.IsPhoneVerified ?? true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _dbContext.UserAccounts.Add(account);

        if (selectedRole == UserRole.RestaurantOwner)
        {
            _dbContext.Restaurants.Add(BuildRestaurant(account.Id, effectiveRequest));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AccountMutationResultDto
        {
            Id = account.Id
        };
    }

    public async Task<AccountMutationResultDto> UpdateAccountAsync(
        Guid accountId,
        SaveAdminAccountRequestDto request,
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

        var restaurant = await _dbContext.Restaurants
            .OrderBy(existingRestaurant => existingRestaurant.CreatedAtUtc)
            .FirstOrDefaultAsync(existingRestaurant => existingRestaurant.OwnerId == account.Id, cancellationToken);

        var effectiveRequest = BuildEffectiveUpdateRequest(account, restaurant, request);
        await ValidateRequestAsync(effectiveRequest, accountId, cancellationToken);
        await EnforceAdminSafetyRulesAsync(account, effectiveRequest, currentAdminUserId, cancellationToken);

        var selectedRole = ParseVisibleRole(effectiveRequest.Role);
        account.FullName = effectiveRequest.FullName!.Trim();
        account.PhoneNumber = effectiveRequest.PhoneNumber!.Trim();
        account.Username = NormalizeUsername(effectiveRequest.Username!);
        account.Role = selectedRole;
        account.IsActive = effectiveRequest.IsActive ?? account.IsActive;
        account.IsPhoneVerified = effectiveRequest.IsPhoneVerified ?? account.IsPhoneVerified;
        account.UpdatedAtUtc = DateTime.UtcNow;

        if (selectedRole == UserRole.RestaurantOwner)
        {
            if (restaurant is null)
            {
                _dbContext.Restaurants.Add(BuildRestaurant(account.Id, effectiveRequest));
            }
            else
            {
                UpdateRestaurant(restaurant, effectiveRequest);
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

        return new AccountMutationResultDto
        {
            Id = accountId
        };
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

        var userCarts = await _dbContext.TableCarts
            .Where(cart => cart.UserId == account.Id)
            .ToListAsync(cancellationToken);

        var hasUserOrderHistory = await _dbContext.Orders
            .AnyAsync(order => order.UserId == account.Id, cancellationToken);

        if (hasUserOrderHistory)
        {
            throw new AdminAccountServiceException(
                "This account cannot be hard-deleted because it is referenced by existing orders.");
        }

        foreach (var ownedRestaurant in ownedRestaurants)
        {
            var hasRestaurantOrders = await _dbContext.Orders
                .AnyAsync(order => order.RestaurantId == ownedRestaurant.Id, cancellationToken);

            if (hasRestaurantOrders)
            {
                throw new AdminAccountServiceException(
                    "This restaurant owner cannot be hard-deleted because the linked restaurant has order history.");
            }

            var hasMenuItemOrderHistory = await _dbContext.OrderItems
                .AnyAsync(orderItem => _dbContext.MenuItems
                    .Where(menuItem => menuItem.Id == orderItem.MenuItemId)
                    .Any(menuItem => menuItem.RestaurantId == ownedRestaurant.Id), cancellationToken);

            if (hasMenuItemOrderHistory)
            {
                throw new AdminAccountServiceException(
                    "This restaurant owner cannot be hard-deleted because linked menu items are referenced by order history.");
            }

            var hasTableOrderHistory = await _dbContext.Orders
                .AnyAsync(order => _dbContext.RestaurantTables
                    .Where(table => table.Id == order.RestaurantTableId)
                    .Any(table => table.RestaurantId == ownedRestaurant.Id), cancellationToken);

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

                var restaurantMenuCategories = await _dbContext.MenuCategories
                    .Where(category => restaurantIds.Contains(category.RestaurantId))
                    .ToListAsync(cancellationToken);

                var restaurantMenuItems = await _dbContext.MenuItems
                    .Where(menuItem => restaurantIds.Contains(menuItem.RestaurantId))
                    .ToListAsync(cancellationToken);

                var restaurantTables = await _dbContext.RestaurantTables
                    .Where(table => restaurantIds.Contains(table.RestaurantId))
                    .ToListAsync(cancellationToken);

                var restaurantCarts = await _dbContext.TableCarts
                    .Where(cart => restaurantIds.Contains(cart.RestaurantId))
                    .ToListAsync(cancellationToken);

                if (restaurantCarts.Count > 0)
                {
                    _dbContext.TableCarts.RemoveRange(restaurantCarts);
                }

                if (restaurantMenuItems.Count > 0)
                {
                    _dbContext.MenuItems.RemoveRange(restaurantMenuItems);
                }

                if (restaurantMenuCategories.Count > 0)
                {
                    _dbContext.MenuCategories.RemoveRange(restaurantMenuCategories);
                }

                if (restaurantTables.Count > 0)
                {
                    _dbContext.RestaurantTables.RemoveRange(restaurantTables);
                }

                _dbContext.Restaurants.RemoveRange(ownedRestaurants);
            }

            if (userCarts.Count > 0)
            {
                _dbContext.TableCarts.RemoveRange(userCarts);
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

    private static SaveAdminAccountRequestDto ApplyCreateDefaults(SaveAdminAccountRequestDto request)
    {
        return new SaveAdminAccountRequestDto
        {
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Username = request.Username,
            Password = request.Password,
            ConfirmPassword = request.ConfirmPassword,
            Role = request.Role,
            IsActive = request.IsActive ?? true,
            IsPhoneVerified = request.IsPhoneVerified ?? true,
            RestaurantName = request.RestaurantName,
            RestaurantDescription = request.RestaurantDescription,
            RestaurantAddress = request.RestaurantAddress
        };
    }

    private static SaveAdminAccountRequestDto BuildEffectiveUpdateRequest(
        UserAccount account,
        Restaurant? restaurant,
        SaveAdminAccountRequestDto request)
    {
        return new SaveAdminAccountRequestDto
        {
            FullName = request.FullName?.Trim() ?? account.FullName,
            PhoneNumber = request.PhoneNumber?.Trim() ?? account.PhoneNumber,
            Username = request.Username?.Trim() ?? account.Username,
            Role = request.Role ?? account.Role.ToString(),
            IsActive = request.IsActive ?? account.IsActive,
            IsPhoneVerified = request.IsPhoneVerified ?? account.IsPhoneVerified,
            RestaurantName = request.RestaurantName?.Trim() ?? restaurant?.Name,
            RestaurantDescription = request.RestaurantDescription?.Trim() ?? restaurant?.Description,
            RestaurantAddress = request.RestaurantAddress?.Trim() ?? restaurant?.Address
        };
    }

    private async Task ValidateRequestAsync(
        SaveAdminAccountRequestDto request,
        Guid? currentAccountId,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            errors[nameof(request.FullName)] = ["Full name is required."];
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            errors[nameof(request.PhoneNumber)] = ["Phone number is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            errors[nameof(request.Username)] = ["Username is required."];
        }

        if (!TryParseVisibleRole(request.Role, out _))
        {
            errors[nameof(request.Role)] = ["Please select a valid role."];
        }

        var normalizedPhone = request.PhoneNumber?.Trim() ?? string.Empty;
        var normalizedUsername = NormalizeUsername(request.Username ?? string.Empty);
        var normalizedRestaurantName = string.IsNullOrWhiteSpace(request.RestaurantName) ? null : request.RestaurantName.Trim();
        var normalizedRestaurantDescription = string.IsNullOrWhiteSpace(request.RestaurantDescription) ? null : request.RestaurantDescription.Trim();
        var normalizedRestaurantAddress = string.IsNullOrWhiteSpace(request.RestaurantAddress) ? null : request.RestaurantAddress.Trim();

        if (!currentAccountId.HasValue)
        {
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                errors[nameof(request.Password)] = ["Password is required when creating an account."];
            }
            else if (request.Password.Length < 6)
            {
                errors[nameof(request.Password)] = ["Password must be at least 6 characters long."];
            }

            if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                errors[nameof(request.ConfirmPassword)] = ["Please confirm the password."];
            }
            else if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
            {
                errors[nameof(request.ConfirmPassword)] = ["Confirm password must match the password."];
            }
        }

        var phoneExists = await _dbContext.UserAccounts
            .AnyAsync(
                user => user.PhoneNumber == normalizedPhone && user.Id != currentAccountId,
                cancellationToken);

        if (phoneExists)
        {
            errors[nameof(request.PhoneNumber)] = ["Another account already uses this phone number."];
        }

        var usernameExists = await _dbContext.UserAccounts
            .AnyAsync(
                user => user.Username == normalizedUsername && user.Id != currentAccountId,
                cancellationToken);

        if (usernameExists)
        {
            errors[nameof(request.Username)] = ["Another account already uses this username."];
        }

        if (TryParseVisibleRole(request.Role, out var selectedRole) && selectedRole == UserRole.RestaurantOwner)
        {
            if (string.IsNullOrWhiteSpace(normalizedRestaurantName))
            {
                errors[nameof(request.RestaurantName)] = ["Restaurant name is required for restaurant owners."];
            }

            if (string.IsNullOrWhiteSpace(normalizedRestaurantDescription))
            {
                errors[nameof(request.RestaurantDescription)] = ["Restaurant description is required for restaurant owners."];
            }

            if (string.IsNullOrWhiteSpace(normalizedRestaurantAddress))
            {
                errors[nameof(request.RestaurantAddress)] = ["Restaurant address is required for restaurant owners."];
            }
        }

        if (errors.Count > 0)
        {
            throw new AdminAccountServiceException("Validation failed.", errors: errors);
        }
    }

    private async Task EnforceAdminSafetyRulesAsync(
        UserAccount existingAccount,
        SaveAdminAccountRequestDto request,
        Guid? currentAdminUserId,
        CancellationToken cancellationToken)
    {
        if (!TryParseVisibleRole(request.Role, out var newRole))
        {
            return;
        }

        var isDemotingAdmin = existingAccount.Role == UserRole.Admin && newRole != UserRole.Admin;
        var isDeactivatingAdmin = existingAccount.Role == UserRole.Admin && existingAccount.IsActive && request.IsActive == false;

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
                [nameof(SaveAdminAccountRequestDto.Role)] = ["Please select a valid role."]
            });
    }

    private static Restaurant BuildRestaurant(Guid ownerId, SaveAdminAccountRequestDto request)
    {
        return new Restaurant
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = request.RestaurantName!.Trim(),
            Description = request.RestaurantDescription!.Trim(),
            Address = request.RestaurantAddress!.Trim(),
            ContactPhone = request.PhoneNumber!.Trim(),
            ApprovalStatus = RestaurantApprovalStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static void UpdateRestaurant(Restaurant restaurant, SaveAdminAccountRequestDto request)
    {
        restaurant.Name = request.RestaurantName!.Trim();
        restaurant.Description = request.RestaurantDescription!.Trim();
        restaurant.Address = request.RestaurantAddress!.Trim();
        restaurant.ContactPhone = request.PhoneNumber!.Trim();
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().ToLowerInvariant();
    }
}
