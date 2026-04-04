using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Infrastructure.Data;
using SmartDiningSystem.Infrastructure.Data.Seed;

namespace SmartDiningSystem.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    private static readonly Type[] RequiredEntityTypes =
    [
        typeof(UserAccount),
        typeof(Restaurant),
        typeof(RestaurantTable),
        typeof(MenuCategory),
        typeof(MenuItem),
        typeof(Order),
        typeof(OrderItem),
        typeof(Booking),
        typeof(BookingItem),
        typeof(TableSession),
        typeof(PendingRegistration),
        typeof(OtpCode),
        typeof(RestaurantRating),
        typeof(TableCart),
        typeof(TableCartItem)
    ];

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
            var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();

            logger.LogInformation(
                "Starting database migration. Applied migrations: {AppliedMigrationCount}. Pending migrations: {PendingMigrationCount}.",
                appliedMigrations.Length,
                pendingMigrations.Length);

            if (pendingMigrations.Length > 0)
            {
                logger.LogInformation("Pending migrations: {PendingMigrations}", string.Join(", ", pendingMigrations));
            }

            logger.LogInformation("Starting database migration");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Migration completed");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database migration failed. Seed and schema validation will not run.");
            throw;
        }

        try
        {
            logger.LogInformation("Starting pre-seed schema validation");
            await ValidateRequiredSchemaAsync(dbContext, logger);
            logger.LogInformation("Pre-seed schema validation completed");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Pre-seed schema validation failed after database migration. Seed will not run.");
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

        logger.LogInformation("Starting post-seed schema validation");
        await ValidateRequiredSchemaAsync(dbContext, logger);
        logger.LogInformation("Post-seed schema validation completed");
    }

    private static async Task ValidateRequiredSchemaAsync(AppDbContext dbContext, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(logger);

        var requirements = BuildSchemaRequirements(dbContext);

        if (requirements.Count == 0)
        {
            throw new InvalidOperationException("Required schema validation could not determine any schema requirements from the current EF model.");
        }

        var connection = dbContext.Database.GetDbConnection();
        var currentSchema = await ExecuteWithOpenConnectionAsync(
            connection,
            async openConnection =>
            {
                var schema = await GetCurrentSchemaAsync(openConnection);
                var existingTables = await GetExistingTablesAsync(openConnection);
                var existingColumns = await GetExistingColumnsAsync(openConnection);

                var missingTables = new List<string>();
                var missingColumns = new List<string>();

                foreach (var requirement in requirements)
                {
                    var schemaName = requirement.Schema ?? schema;
                    var tableKey = BuildTableKey(schemaName, requirement.TableName);

                    if (!existingTables.Contains(tableKey))
                    {
                        missingTables.Add(tableKey);
                        continue;
                    }

                    foreach (var columnName in requirement.ColumnNames)
                    {
                        var columnKey = BuildColumnKey(schemaName, requirement.TableName, columnName);
                        if (!existingColumns.Contains(columnKey))
                        {
                            missingColumns.Add(columnKey);
                        }
                    }
                }

                if (missingTables.Count > 0 || missingColumns.Count > 0)
                {
                    var messageParts = new List<string>();

                    if (missingTables.Count > 0)
                    {
                        messageParts.Add($"Missing tables: {string.Join(", ", missingTables.OrderBy(table => table, StringComparer.Ordinal))}.");
                    }

                    if (missingColumns.Count > 0)
                    {
                        messageParts.Add($"Missing columns: {string.Join(", ", missingColumns.OrderBy(column => column, StringComparer.Ordinal))}.");
                    }

                    throw new InvalidOperationException(
                        $"Database schema is inconsistent after migration. {string.Join(" ", messageParts)}");
                }

                return schema;
            });

        try
        {
            await dbContext.UserAccounts.AsNoTracking().AnyAsync();
            await dbContext.Restaurants.AsNoTracking().AnyAsync();
            await dbContext.MenuCategories.AsNoTracking().AnyAsync();
            await dbContext.MenuItems.AsNoTracking().AnyAsync();
            await dbContext.RestaurantTables.AsNoTracking().AnyAsync();
            await dbContext.Bookings.AsNoTracking().AnyAsync();
            await dbContext.TableSessions.AsNoTracking().AnyAsync();
            await dbContext.Orders.AsNoTracking().AnyAsync();
            await dbContext.PendingRegistrations.AsNoTracking().AnyAsync();
            await dbContext.OtpCodes.AsNoTracking().AnyAsync();
            await dbContext.RestaurantRatings.AsNoTracking().AnyAsync();

            var requiredColumnCount = requirements.Sum(requirement => requirement.ColumnNames.Count);

            logger.LogInformation(
                "Required schema validation completed successfully for schema {Schema}. Tables verified: {TableCount}. Columns verified: {ColumnCount}.",
                currentSchema,
                requirements.Count,
                requiredColumnCount);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Required schema validation failed. Startup will be stopped.");
            throw;
        }
    }

    private static IReadOnlyList<TableSchemaRequirement> BuildSchemaRequirements(AppDbContext dbContext)
    {
        var requirements = new List<TableSchemaRequirement>(RequiredEntityTypes.Length);

        foreach (var entityClrType in RequiredEntityTypes)
        {
            var entityType = dbContext.Model.FindEntityType(entityClrType)
                ?? throw new InvalidOperationException(
                    $"Required entity type {entityClrType.Name} is missing from the current EF Core model.");

            var tableName = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new InvalidOperationException(
                    $"Required entity type {entityClrType.Name} does not map to a relational table.");
            }

            var tableIdentifier = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
            var columnNames = entityType
                .GetProperties()
                .Select(property => property.GetColumnName(tableIdentifier))
                .Where(columnName => !string.IsNullOrWhiteSpace(columnName))
                .Select(columnName => columnName!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(columnName => columnName, StringComparer.Ordinal)
                .ToArray();

            if (columnNames.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Required entity type {entityClrType.Name} did not produce any mapped columns.");
            }

            requirements.Add(new TableSchemaRequirement(tableName, entityType.GetSchema(), columnNames));
        }

        return requirements;
    }

    private static async Task<T> ExecuteWithOpenConnectionAsync<T>(
        DbConnection connection,
        Func<DbConnection, Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(operation);

        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync();
        }

        try
        {
            return await operation(connection);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<string> GetCurrentSchemaAsync(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "select current_schema()";

        var result = await command.ExecuteScalarAsync();
        return string.IsNullOrWhiteSpace(result as string) ? "public" : (string)result;
    }

    private static async Task<HashSet<string>> GetExistingTablesAsync(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select table_schema, table_name
            from information_schema.tables
            where table_type = 'BASE TABLE'
            """;

        var existingTables = new HashSet<string>(StringComparer.Ordinal);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            existingTables.Add(BuildTableKey(reader.GetString(0), reader.GetString(1)));
        }

        return existingTables;
    }

    private static async Task<HashSet<string>> GetExistingColumnsAsync(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select table_schema, table_name, column_name
            from information_schema.columns
            """;

        var existingColumns = new HashSet<string>(StringComparer.Ordinal);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            existingColumns.Add(BuildColumnKey(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }

        return existingColumns;
    }

    private static string BuildTableKey(string schema, string tableName)
    {
        return $"{schema}.{tableName}";
    }

    private static string BuildColumnKey(string schema, string tableName, string columnName)
    {
        return $"{schema}.{tableName}.{columnName}";
    }

    private sealed record TableSchemaRequirement(
        string TableName,
        string? Schema,
        IReadOnlyList<string> ColumnNames);
}
