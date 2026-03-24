using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Npgsql;
using SmartDiningSystem.Api.Extensions;
using SmartDiningSystem.Application.DTOs.Common;

var builder = WebApplication.CreateBuilder(args);

NormalizeDatabaseConfiguration(builder);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllersWithViews();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new ApiErrorResponseDto
        {
            Message = "Validation failed.",
            Errors = errors,
            TraceId = context.HttpContext.TraceIdentifier
        });
    };
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter: Bearer {accessToken}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [jwtSecurityScheme] = Array.Empty<string>()
    });
});

builder.Services.AddAuthorization();
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

await app.ApplyDatabaseSetupAsync();

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "admin_login",
    pattern: "mainadmin/login",
    defaults: new { area = "Admin", controller = "Auth", action = "Login" });

app.MapControllerRoute(
    name: "admin_area",
    pattern: "mainadmin/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "Admin" });

app.Run();

static void NormalizeDatabaseConfiguration(WebApplicationBuilder builder)
{
    ArgumentNullException.ThrowIfNull(builder);

    var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (LooksLikePostgresUrl(configuredConnectionString))
    {
        var normalizedConnectionString =
            BuildNpgsqlConnectionStringFromDatabaseUrl(configuredConnectionString!);

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = normalizedConnectionString
        });
    }

    var portValue = Environment.GetEnvironmentVariable("PORT");
    if (int.TryParse(portValue, out var port) && port > 0)
    {
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }
}

static bool LooksLikePostgresUrl(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return false;
    }

    return connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        || connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);
}

static string BuildNpgsqlConnectionStringFromDatabaseUrl(string databaseUrl)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(databaseUrl);

    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var databaseUri))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is not a valid absolute PostgreSQL URI.");
    }

    if (!string.Equals(databaseUri.Scheme, "postgres", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(databaseUri.Scheme, "postgresql", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection must use the postgres or postgresql scheme.");
    }

    if (string.IsNullOrWhiteSpace(databaseUri.Host))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection must include a PostgreSQL host.");
    }

    var databaseName = Uri.UnescapeDataString(databaseUri.AbsolutePath.Trim('/'));
    if (string.IsNullOrWhiteSpace(databaseName))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection must include a PostgreSQL database name.");
    }

    var userInfoParts = databaseUri.UserInfo.Split(':', 2, StringSplitOptions.None);
    if (userInfoParts.Length != 2
        || string.IsNullOrWhiteSpace(userInfoParts[0])
        || string.IsNullOrWhiteSpace(userInfoParts[1]))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection must include both username and password.");
    }

    var connectionStringBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = databaseUri.Host,
        Port = databaseUri.Port > 0 ? databaseUri.Port : 5432,
        Database = databaseName,
        Username = Uri.UnescapeDataString(userInfoParts[0]),
        Password = Uri.UnescapeDataString(userInfoParts[1]),
        SslMode = SslMode.Require,
        TrustServerCertificate = true
    };

    var queryParameters = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(databaseUri.Query);
    if (queryParameters.TryGetValue("sslmode", out var sslModeValue)
        && Enum.TryParse<SslMode>(sslModeValue.ToString(), true, out var sslMode))
    {
        connectionStringBuilder.SslMode = sslMode;
    }

    if (queryParameters.TryGetValue("trustservercertificate", out var trustServerCertificateValue)
        && bool.TryParse(trustServerCertificateValue.ToString(), out var trustServerCertificate))
    {
        connectionStringBuilder.TrustServerCertificate = trustServerCertificate;
    }

    return connectionStringBuilder.ConnectionString;
}
