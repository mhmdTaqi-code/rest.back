using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
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

        await ValidateRequiredSchemaAsync(dbContext, logger);

        var adminSeedService = services.GetRequiredService<AdminSeedService>();
        logger.LogInformation("Starting seed");
        await adminSeedService.SeedAsync();
        logger.LogInformation("Seed completed");
    }

    private static async Task ValidateRequiredSchemaAsync(AppDbContext dbContext, ILogger logger)
    {
        try
        {
            await dbContext.UserAccounts.AsNoTracking().AnyAsync();
            await dbContext.Restaurants.AsNoTracking().AnyAsync();
            await dbContext.MenuCategories.AsNoTracking().AnyAsync();
            await dbContext.MenuItems.AsNoTracking().AnyAsync();
            await dbContext.RestaurantTables.AsNoTracking().AnyAsync();
            logger.LogInformation("Required schema validation completed successfully.");
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            logger.LogError(
                exception,
                "Required schema validation failed after database migration. Startup will be stopped.");
            throw new InvalidOperationException(
                "Database schema is inconsistent after migration. Required tables are still missing.",
                exception);
        }
    }
}
