using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using SmartDiningSystem.Api.Extensions;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Infrastructure.Data;

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
    if (!string.IsNullOrWhiteSpace(configuredConnectionString))
    {
        var normalizedConnectionString = PostgresConnectionStringResolver.NormalizeIfNeeded(
            configuredConnectionString,
            "ConnectionStrings:DefaultConnection");

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
