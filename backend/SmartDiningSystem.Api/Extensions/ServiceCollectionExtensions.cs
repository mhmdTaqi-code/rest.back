using FluentValidation;
using FluentValidation.AspNetCore;
using System.IO;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartDiningSystem.Application.Configuration;
using SmartDiningSystem.Infrastructure.Data;
using SmartDiningSystem.Infrastructure.Data.Seed;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Application.Validation.Auth;
using SmartDiningSystem.Infrastructure.Services;

namespace SmartDiningSystem.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString =
                "Host=localhost;Port=5433;Database=SmartDiningDb;Username=postgres;Password=123456";
        }

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<IraqOtpOptions>(configuration.GetSection(IraqOtpOptions.SectionName));
        services.Configure<AdminDevelopmentCredentialsOptions>(
            configuration.GetSection(AdminDevelopmentCredentialsOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
        {
            jwtOptions.Issuer = "SmartDiningSystem";
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
        {
            jwtOptions.Audience = "SmartDiningSystem.Client";
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) || jwtOptions.SecretKey.Length < 32)
        {
            jwtOptions.SecretKey = "CHANGE_ME_WITH_A_LONG_RANDOM_SECRET";
        }

        var dataProtectionKeysPath = Path.Combine(AppContext.BaseDirectory, "DataProtectionKeys");
        Directory.CreateDirectory(dataProtectionKeysPath);

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
            .SetApplicationName("SmartDiningSystem.Api");

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<RegisterUserRequestDtoValidator>();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly("SmartDiningSystem.Infrastructure")));

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = AdminAuthenticationDefaults.PolicyScheme;
                options.DefaultAuthenticateScheme = AdminAuthenticationDefaults.PolicyScheme;
                options.DefaultChallengeScheme = AdminAuthenticationDefaults.PolicyScheme;
            })
            .AddPolicyScheme(AdminAuthenticationDefaults.PolicyScheme, "Smart Dining authentication", options =>
            {
                options.ForwardDefaultSelector = context =>
                    context.Request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase)
                        ? AdminAuthenticationDefaults.CookieScheme
                        : JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie(AdminAuthenticationDefaults.CookieScheme, options =>
            {
                options.Cookie.Name = "SmartDining.Admin";
                options.LoginPath = "/mainadmin/login";
                options.AccessDeniedPath = "/mainadmin/login";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    RoleClaimType = ClaimTypes.Role,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<IAdminAuthenticationService, AdminAuthenticationService>();
        services.AddScoped<IAdminAccountService, AdminAccountService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IAdminRestaurantService, AdminRestaurantService>();
        services.AddScoped<IRestaurantQueryService, RestaurantQueryService>();
        services.AddScoped<IRestaurantTableAccessService, RestaurantTableAccessService>();
        services.AddScoped<IPublicTableMenuService, PublicTableMenuService>();
        services.AddScoped<ITableCartService, TableCartService>();
        services.AddScoped<ITableOrderService, TableOrderService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddHttpClient<IOtpService, IraqOtpService>((serviceProvider, client) =>
        {
            var iraqOtpOptions = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<IraqOtpOptions>>()
                .Value;

            if (Uri.TryCreate(iraqOtpOptions.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                client.BaseAddress = baseUri;
            }

            client.DefaultRequestHeaders.Accept.Clear();
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<AdminSeedService>();

        return services;
    }
}
