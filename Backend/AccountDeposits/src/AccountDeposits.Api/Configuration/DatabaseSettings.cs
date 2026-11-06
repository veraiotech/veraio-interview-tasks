using Npgsql;

namespace AccountDeposits.Api.Configuration;

public static class DatabaseSettings
{
    public const string HostKey = "POSTGRES_HOST";
    public const string PortKey = "POSTGRES_PORT";
    public const string DatabaseKey = "POSTGRES_DB";
    public const string UserKey = "POSTGRES_USER";
    public const string PasswordKey = "POSTGRES_PASSWORD";

    private const string DefaultHost = "localhost";
    private const int DefaultPort = 5433;

    /// <summary>Builds the connection string, or throws when a required setting is missing.</summary>
    public static string GetConnectionString(IConfiguration configuration)
    {
        string[] required = [DatabaseKey, UserKey, PasswordKey];
        var missing = required.Where(key => string.IsNullOrWhiteSpace(configuration[key])).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing required configuration: {string.Join(", ", missing)}. " +
                "Copy .env.example to .env or set them as environment variables.");
        }

        var host = configuration[HostKey];

        return new NpgsqlConnectionStringBuilder
        {
            Host = string.IsNullOrWhiteSpace(host) ? DefaultHost : host,
            Port = configuration.GetValue<int?>(PortKey) ?? DefaultPort,
            Database = configuration[DatabaseKey],
            Username = configuration[UserKey],
            Password = configuration[PasswordKey],
        }.ConnectionString;
    }
}
