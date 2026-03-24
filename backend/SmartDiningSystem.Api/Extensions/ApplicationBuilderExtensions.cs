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
            logger.LogError(exception, "Database migration failed. Seed and schema validation will not run.");
            throw;
        }

        var adminSeedService = services.GetRequiredService<AdminSeedService>();
        try
        {
            logger.LogInformation("Starting seed");
            await adminSeedService.SeedAsync();
            logger.LogInformation("Seed completed");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database seed failed. Schema validation will not run.");
            throw;
        }

        logger.LogInformation("Starting schema validation");
        await ValidateRequiredSchemaAsync(dbContext, logger);
        logger.LogInformation("Schema validation completed");
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
                "Required schema validation failed after database migration and seed. Startup will be stopped.");
            throw new InvalidOperationException(
                "Database schema is inconsistent after migration and seed. Required tables are still missing.",
                exception);
        }
    }
}
