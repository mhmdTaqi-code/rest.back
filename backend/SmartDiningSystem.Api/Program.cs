using Microsoft.Extensions.Configuration;
using Npgsql;
using SmartDiningSystem.Api.Extensions;
using SmartDiningSystem.Application.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

ApplyRenderConfiguration(builder);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// دعم PORT من Render
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// استخدم DATABASE_URL مباشرة بدون parsing
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] = databaseUrl;
}

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

static void ApplyRenderConfiguration(WebApplicationBuilder builder)
{
    ArgumentNullException.ThrowIfNull(builder);

    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        var connectionString = BuildNpgsqlConnectionStringFromDatabaseUrl(databaseUrl);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connectionString
        });
    }

    var portValue = Environment.GetEnvironmentVariable("PORT");
    if (int.TryParse(portValue, out var port) && port > 0)
    {
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }
}

static string BuildNpgsqlConnectionStringFromDatabaseUrl(string databaseUrl)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(databaseUrl);

    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var databaseUri))
    {
        throw new InvalidOperationException("DATABASE_URL is not a valid absolute URI.");
    }

    if (!string.Equals(databaseUri.Scheme, "postgres", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(databaseUri.Scheme, "postgresql", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("DATABASE_URL must use the postgres or postgresql scheme.");
    }

    if (string.IsNullOrWhiteSpace(databaseUri.Host))
    {
        throw new InvalidOperationException("DATABASE_URL must include a PostgreSQL host.");
    }

    var databaseName = Uri.UnescapeDataString(databaseUri.AbsolutePath.Trim('/'));
    if (string.IsNullOrWhiteSpace(databaseName))
    {
        throw new InvalidOperationException("DATABASE_URL must include a PostgreSQL database name.");
    }

    var userInfoParts = databaseUri.UserInfo.Split(':', 2, StringSplitOptions.None);
    if (userInfoParts.Length != 2
        || string.IsNullOrWhiteSpace(userInfoParts[0])
        || string.IsNullOrWhiteSpace(userInfoParts[1]))
    {
        throw new InvalidOperationException("DATABASE_URL must include both username and password.");
    }

    var builder = new NpgsqlConnectionStringBuilder
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
        builder.SslMode = sslMode;
    }

    if (queryParameters.TryGetValue("trust server certificate", out var trustServerCertificateValue)
        && bool.TryParse(trustServerCertificateValue.ToString(), out var trustServerCertificate))
    {
        builder.TrustServerCertificate = trustServerCertificate;
    }

    if (queryParameters.TryGetValue("trustservercertificate", out trustServerCertificateValue)
        && bool.TryParse(trustServerCertificateValue.ToString(), out trustServerCertificate))
    {
        builder.TrustServerCertificate = trustServerCertificate;
    }

    return builder.ConnectionString;
}
