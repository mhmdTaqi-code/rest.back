using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartDiningSystem.Application.Configuration;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Infrastructure.Data;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Infrastructure.Services;

public class AdminAuthenticationService : IAdminAuthenticationService
{
    public static readonly Guid DevelopmentAdminId = Guid.Parse("00000000-0000-0000-0000-000000000012");
    public const string DevelopmentAdminPhone = "12";
    public const string DevelopmentAdminFullName = "Development Main Admin";

    private readonly AdminDevelopmentCredentialsOptions _credentialsOptions;
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AdminAuthenticationService> _logger;

    public AdminAuthenticationService(
        IOptions<AdminDevelopmentCredentialsOptions> credentialsOptions,
        AppDbContext dbContext,
        IWebHostEnvironment environment,
        ILogger<AdminAuthenticationService> logger)
    {
        _credentialsOptions = credentialsOptions.Value;
        _dbContext = dbContext;
        _environment = environment;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        var configuredUsername = _credentialsOptions.Username;
        var configuredPassword = _credentialsOptions.Password;
        var usernameEnvironmentValue = Environment.GetEnvironmentVariable(
            $"{AdminDevelopmentCredentialsOptions.SectionName}__Username");
        var passwordEnvironmentValue = Environment.GetEnvironmentVariable(
            $"{AdminDevelopmentCredentialsOptions.SectionName}__Password");

        var hasConfiguredUsername = !string.IsNullOrWhiteSpace(configuredUsername);
        var hasConfiguredPassword = !string.IsNullOrWhiteSpace(configuredPassword);
        var usernameEnvironmentVariablePresent = usernameEnvironmentValue is not null;
        var passwordEnvironmentVariablePresent = passwordEnvironmentValue is not null;

        _logger.LogInformation(
            "Main admin login attempt. Environment={EnvironmentName}, HasConfiguredUsername={HasConfiguredUsername}, HasConfiguredPassword={HasConfiguredPassword}, UsernameEnvVarPresent={UsernameEnvVarPresent}, PasswordEnvVarPresent={PasswordEnvVarPresent}, UsernameEnvVarEmpty={UsernameEnvVarEmpty}, PasswordEnvVarEmpty={PasswordEnvVarEmpty}.",
            _environment.EnvironmentName,
            hasConfiguredUsername,
            hasConfiguredPassword,
            usernameEnvironmentVariablePresent,
            passwordEnvironmentVariablePresent,
            usernameEnvironmentVariablePresent && string.IsNullOrWhiteSpace(usernameEnvironmentValue),
            passwordEnvironmentVariablePresent && string.IsNullOrWhiteSpace(passwordEnvironmentValue));

        if (!hasConfiguredUsername || !hasConfiguredPassword)
        {
            _logger.LogError(
                "Main admin credentials are not configured. Environment={EnvironmentName}, HasConfiguredUsername={HasConfiguredUsername}, HasConfiguredPassword={HasConfiguredPassword}.",
                _environment.EnvironmentName,
                hasConfiguredUsername,
                hasConfiguredPassword);

            throw new AdminAuthenticationConfigurationException(
                "Main admin credentials are not configured for this environment.");
        }

        if (!string.Equals(username, configuredUsername, StringComparison.Ordinal) ||
            !string.Equals(password, configuredPassword, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Main admin login failed due to credential mismatch. Environment={EnvironmentName}, HasConfiguredUsername={HasConfiguredUsername}, HasConfiguredPassword={HasConfiguredPassword}.",
                _environment.EnvironmentName,
                hasConfiguredUsername,
                hasConfiguredPassword);
            return null;
        }

        var adminAccount = await _dbContext.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => (user.Id == DevelopmentAdminId ||
                         user.PhoneNumber == DevelopmentAdminPhone) &&
                        user.Role == UserRole.Admin &&
                        user.IsActive,
                cancellationToken);

        if (adminAccount is null)
        {
            _logger.LogError(
                "Main admin credentials matched configuration, but the backing admin account was not found or inactive. Environment={EnvironmentName}.",
                _environment.EnvironmentName);
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
