using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Npgsql;
using SmartDiningSystem.Api.Extensions;
using SmartDiningSystem.Application.DTOs.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Support Render PORT binding
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

// Support Render DATABASE_URL if present
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    var connectionString = BuildNpgsqlConnectionStringFromDatabaseUrl(databaseUrl);
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
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

static string BuildNpgsqlConnectionStringFromDatabaseUrl(string databaseUrl)
{
    if (string.IsNullOrWhiteSpace(databaseUrl))
        throw new InvalidOperationException("DATABASE_URL is missing or empty.");

    // Render عادة تعطي postgresql://...
    // Uri يتعامل بشكل أفضل إذا كانت الصيغة postgres:// أو postgresql://
    if (databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
    {
        databaseUrl = "postgresql://" + databaseUrl["postgres://".Length..];
    }

    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
        throw new InvalidOperationException("DATABASE_URL is not a valid absolute URI.");

    if (string.IsNullOrWhiteSpace(uri.Host))
        throw new InvalidOperationException("DATABASE_URL does not contain a valid host.");

    var database = uri.AbsolutePath.Trim('/');
    if (string.IsNullOrWhiteSpace(database))
        throw new InvalidOperationException("DATABASE_URL does not contain a database name.");

    var userInfo = uri.UserInfo.Split(':', 2, StringSplitOptions.None);
    if (userInfo.Length == 0 || string.IsNullOrWhiteSpace(userInfo[0]))
        throw new InvalidOperationException("DATABASE_URL does not contain a valid username.");

    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
    var port = uri.Port > 0 ? uri.Port : 5432;

    var csb = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = port,
        Database = database,
        Username = username,
        Password = password,
        SslMode = SslMode.Require,
        TrustServerCertificate = true,
        IncludeErrorDetail = true,
        Timeout = 15,
        CommandTimeout = 30,
        Pooling = true
    };

    return csb.ConnectionString;
}
