using AccountDeposits.Api.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AccountDeposits.E2ETests;

/// <summary>
/// Hosts the API in the "Testing" environment, so the developer's local .env is never read,
/// and supplies every required setting explicitly.
/// </summary>
public class AccountDepositsApiFactory : WebApplicationFactory<Program>
{
    private static readonly IReadOnlyDictionary<string, string> DefaultSettings = new Dictionary<string, string>
    {
        [DatabaseSettings.HostKey] = "test-host",
        [DatabaseSettings.PortKey] = "5432",
        [DatabaseSettings.DatabaseKey] = "test-db",
        [DatabaseSettings.UserKey] = "test-user",
        [DatabaseSettings.PasswordKey] = "test-password",
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        foreach (var (key, value) in DefaultSettings)
        {
            builder.UseSetting(key, value);
        }
    }
}
