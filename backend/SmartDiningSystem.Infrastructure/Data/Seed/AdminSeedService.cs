using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Infrastructure.Data;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Services;

namespace SmartDiningSystem.Infrastructure.Data.Seed;

public class AdminSeedService
{
    private readonly AppDbContext _dbContext;

    public AdminSeedService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingAdmin = await _dbContext.UserAccounts
            .FirstOrDefaultAsync(user => user.Id == AdminAuthenticationService.DevelopmentAdminId, cancellationToken);

        if (existingAdmin is null)
        {
            existingAdmin = await _dbContext.UserAccounts
                .FirstOrDefaultAsync(
                    user => user.PhoneNumber == AdminAuthenticationService.DevelopmentAdminPhone ||
                            user.Email == AdminAuthenticationService.DevelopmentAdminEmail,
                    cancellationToken);
        }

        if (existingAdmin is null)
        {
            existingAdmin = new UserAccount
            {
                Id = AdminAuthenticationService.DevelopmentAdminId,
                FullName = AdminAuthenticationService.DevelopmentAdminFullName,
                PhoneNumber = AdminAuthenticationService.DevelopmentAdminPhone,
                Email = AdminAuthenticationService.DevelopmentAdminEmail,
                Username = AdminAuthenticationService.DevelopmentAdminPhone,
                PasswordHash = "DEV_ADMIN_COOKIE_AUTH_ONLY",
                Role = UserRole.Admin,
                IsActive = true,
                IsPhoneVerified = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _dbContext.UserAccounts.Add(existingAdmin);
        }
        else
        {
            existingAdmin.FullName = AdminAuthenticationService.DevelopmentAdminFullName;
            existingAdmin.PhoneNumber = AdminAuthenticationService.DevelopmentAdminPhone;
            existingAdmin.Email = AdminAuthenticationService.DevelopmentAdminEmail;
            existingAdmin.Username = AdminAuthenticationService.DevelopmentAdminPhone;
            existingAdmin.PasswordHash = "DEV_ADMIN_COOKIE_AUTH_ONLY";
            existingAdmin.Role = UserRole.Admin;
            existingAdmin.IsActive = true;
            existingAdmin.IsPhoneVerified = true;
            existingAdmin.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
