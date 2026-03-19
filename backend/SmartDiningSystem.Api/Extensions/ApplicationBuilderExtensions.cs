using Microsoft.EntityFrameworkCore;
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

        var dbContext = services.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        var adminSeedService = services.GetRequiredService<AdminSeedService>();
        await adminSeedService.SeedAsync();
    }
}
