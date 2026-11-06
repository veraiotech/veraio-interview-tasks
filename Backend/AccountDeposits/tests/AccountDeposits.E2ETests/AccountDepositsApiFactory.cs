using System.Globalization;
using AccountDeposits.Api.Configuration;
using AccountDeposits.Api.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace AccountDeposits.E2ETests;

/// <summary>
/// Hosts the API in the "Testing" environment, so the developer's local .env is never read,
/// against a throwaway PostgreSQL container that starts empty and is then migrated.
/// </summary>
public sealed class AccountDepositsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string Database = "test-db";
    private const string Username = "test-user";
    private const string Password = "test-password";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase(Database)
        .WithUsername(Username)
        .WithPassword(Password)
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(DatabaseSettings.HostKey, _postgres.Hostname);
        builder.UseSetting(DatabaseSettings.PortKey, _postgres.GetMappedPublicPort(PostgreSqlBuilder.PostgreSqlPort).ToString(CultureInfo.InvariantCulture));
        builder.UseSetting(DatabaseSettings.DatabaseKey, Database);
        builder.UseSetting(DatabaseSettings.UserKey, Username);
        builder.UseSetting(DatabaseSettings.PasswordKey, Password);
    }
}
