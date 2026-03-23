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
        var logger = services.GetRequiredService<ILogger<ApplicationBuilderExtensions>>();

        var dbContext = services.GetRequiredService<AppDbContext>();
        var adminSeedService = services.GetRequiredService<AdminSeedService>();

        try
        {
            logger.LogInformation("Starting database migration");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Migration completed");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database migration failed. Seed will not run.");
            throw;
        }

        logger.LogInformation("Starting seed");
        await adminSeedService.SeedAsync();
        logger.LogInformation("Seed completed");
    }
}
