using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SmartDiningSystem.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var basePath = ResolveConfigurationBasePath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = PostgresConnectionStringResolver.ResolveRequiredConnectionString(configuration);

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            npgsqlOptions.MigrationsAssembly("SmartDiningSystem.Infrastructure"));

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string ResolveConfigurationBasePath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var candidateDirectories = new[]
        {
            currentDirectory,
            Path.GetFullPath(Path.Combine(currentDirectory, "..", "SmartDiningSystem.Api")),
            Path.GetFullPath(Path.Combine(currentDirectory, "backend", "SmartDiningSystem.Api")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SmartDiningSystem.Api")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "backend", "SmartDiningSystem.Api"))
        };

        foreach (var candidateDirectory in candidateDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(Path.Combine(candidateDirectory, "appsettings.json")))
            {
                return candidateDirectory;
            }
        }

        throw new FileNotFoundException("Could not locate appsettings.json for design-time AppDbContext creation.");
    }
}
