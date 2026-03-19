using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using SmartDiningSystem.Application.Configuration;
using SmartDiningSystem.Infrastructure.Data;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Infrastructure.Services;

public class AdminAuthenticationService : IAdminAuthenticationService
{
    public static readonly Guid DevelopmentAdminId = Guid.Parse("00000000-0000-0000-0000-000000000012");
    public const string DevelopmentAdminPhone = "12";
    public const string DevelopmentAdminEmail = "mainadmin12@local.dev";
    public const string DevelopmentAdminFullName = "Development Main Admin";

    private readonly AdminDevelopmentCredentialsOptions _credentialsOptions;
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public AdminAuthenticationService(
        IOptions<AdminDevelopmentCredentialsOptions> credentialsOptions,
        AppDbContext dbContext,
        IWebHostEnvironment environment)
    {
        _credentialsOptions = credentialsOptions.Value;
        _dbContext = dbContext;
        _environment = environment;
    }

    public async Task<ClaimsPrincipal?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return null;
        }

        if (!string.Equals(username, _credentialsOptions.Username, StringComparison.Ordinal) ||
            !string.Equals(password, _credentialsOptions.Password, StringComparison.Ordinal))
        {
            return null;
        }

        var adminAccount = await _dbContext.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => (user.Id == DevelopmentAdminId ||
                         user.PhoneNumber == DevelopmentAdminPhone ||
                         user.Email == DevelopmentAdminEmail) &&
                        user.Role == UserRole.Admin &&
                        user.IsActive,
                cancellationToken);

        if (adminAccount is null)
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, adminAccount.Id.ToString()),
            new("userId", adminAccount.Id.ToString()),
            new(ClaimTypes.Name, adminAccount.FullName),
            new(ClaimTypes.Role, UserRole.Admin.ToString()),
            new("role", UserRole.Admin.ToString())
        };

        var identity = new ClaimsIdentity(claims, AdminAuthenticationDefaults.CookieScheme);
        return new ClaimsPrincipal(identity);
    }
}
