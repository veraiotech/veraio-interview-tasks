using System.Globalization;
using AccountDeposits.Api.Configuration;
using AccountDeposits.Api.Persistence;
using AccountDeposits.Api.Services.Money;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace AccountDeposits.E2ETests;

/// <summary>
/// Hosts the API in the "Testing" environment, so the developer's local .env is never read,
/// against a throwaway PostgreSQL container that starts empty and is then migrated.
/// The clock and the exchange rates are replaced, so the examples in Task.md can be reproduced exactly.
/// </summary>
public sealed class AccountDepositsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>A Monday, so a test can move to later days of the same week or to the next one.</summary>
    public static readonly DateTimeOffset StartTime = new(2026, 11, 9, 10, 0, 0, TimeSpan.Zero);

    private const string Database = "test-db";
    private const string Username = "test-user";
    private const string Password = "test-password";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase(Database)
        .WithUsername(Username)
        .WithPassword(Password)
        .Build();

    /// <summary>A new clock for every test, because a fake clock cannot move back.</summary>
    public FakeTimeProvider Clock { get; private set; } = new(StartTime);

    public FakeExchangeRateProvider Rates { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    /// <summary>Puts back the seeded users and accounts, removes every operation and resets the clock and the rates.</summary>
    public async Task ResetAsync()
    {
        Clock = new FakeTimeProvider(StartTime);
        Rates.IsAvailable = true;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE operations, accounts, users");
        await DatabaseSeeder.SeedAsync(db);
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

        builder.ConfigureTestServices(services =>
        {
            // Resolved per request, so each test sees the clock that ResetAsync created for it.
            services.RemoveAll<TimeProvider>();
            services.AddScoped<TimeProvider>(_ => Clock);

            services.RemoveAll<IExchangeRateProvider>();
            services.AddSingleton<IExchangeRateProvider>(Rates);
        });
    }
}
