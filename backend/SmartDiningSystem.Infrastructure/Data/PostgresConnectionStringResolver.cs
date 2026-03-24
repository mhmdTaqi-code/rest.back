using Microsoft.Extensions.Configuration;
using Npgsql;

namespace SmartDiningSystem.Infrastructure.Data;

public static class PostgresConnectionStringResolver
{
    public static string ResolveRequiredConnectionString(
        IConfiguration configuration,
        string connectionName = "DefaultConnection")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        var configuredConnectionString = configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionName}' is not configured. " +
                $"Set ConnectionStrings__{connectionName} in configuration or the environment.");
        }

        return NormalizeIfNeeded(configuredConnectionString, $"ConnectionStrings:{connectionName}");
    }

    public static string NormalizeIfNeeded(string connectionString, string configurationKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationKey);

        if (!LooksLikePostgresUrl(connectionString))
        {
            return connectionString;
        }

        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var databaseUri))
        {
            throw new InvalidOperationException(
                $"{configurationKey} is not a valid absolute PostgreSQL URI.");
        }

        if (!string.Equals(databaseUri.Scheme, "postgres", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(databaseUri.Scheme, "postgresql", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{configurationKey} must use the postgres or postgresql scheme.");
        }

        if (string.IsNullOrWhiteSpace(databaseUri.Host))
        {
            throw new InvalidOperationException(
                $"{configurationKey} must include a PostgreSQL host.");
        }

        var databaseName = Uri.UnescapeDataString(databaseUri.AbsolutePath.Trim('/'));
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException(
                $"{configurationKey} must include a PostgreSQL database name.");
        }

        var userInfoParts = databaseUri.UserInfo.Split(':', 2, StringSplitOptions.None);
        if (userInfoParts.Length != 2
            || string.IsNullOrWhiteSpace(userInfoParts[0])
            || string.IsNullOrWhiteSpace(userInfoParts[1]))
        {
            throw new InvalidOperationException(
                $"{configurationKey} must include both username and password.");
        }

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = databaseUri.Host,
            Port = databaseUri.Port > 0 ? databaseUri.Port : 5432,
            Database = databaseName,
            Username = Uri.UnescapeDataString(userInfoParts[0]),
            Password = Uri.UnescapeDataString(userInfoParts[1]),
            SslMode = SslMode.Require
        };

        var queryParameters = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(databaseUri.Query);
        if (queryParameters.TryGetValue("sslmode", out var sslModeValue)
            && Enum.TryParse<SslMode>(sslModeValue.ToString(), true, out var sslMode))
        {
            connectionStringBuilder.SslMode = sslMode;
        }

        if (queryParameters.TryGetValue("trustservercertificate", out var trustServerCertificateValue)
            && bool.TryParse(trustServerCertificateValue.ToString(), out var trustServerCertificate))
        {
            connectionStringBuilder.TrustServerCertificate = trustServerCertificate;
        }

        return connectionStringBuilder.ConnectionString;
    }

    public static bool LooksLikePostgresUrl(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        return connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);
    }
}
