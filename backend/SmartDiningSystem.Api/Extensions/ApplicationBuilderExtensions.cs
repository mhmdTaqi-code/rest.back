using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartDiningSystem.Infrastructure.Data;
using SmartDiningSystem.Infrastructure.Data.Seed;

namespace SmartDiningSystem.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task ApplyDatabaseSetupAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Startup.DatabaseSetup");

        var dbContext = services.GetRequiredService<AppDbContext>();
        logger.LogInformation("Starting EF Core database migration.");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("EF Core database migration completed successfully.");

        var adminSeedService = services.GetRequiredService<AdminSeedService>();
        logger.LogInformation("Starting application seed execution.");
        await adminSeedService.SeedAsync();
        logger.LogInformation("Application seed execution completed successfully.");
    }
}
