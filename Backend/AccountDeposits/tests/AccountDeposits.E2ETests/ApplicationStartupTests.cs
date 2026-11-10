using AccountDeposits.Api.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace AccountDeposits.E2ETests;

[Collection(ApiCollection.Name)]
public sealed class ApplicationStartupTests(AccountDepositsApiFactory factory)
{
    [Fact]
    public void Application_refuses_to_start_when_required_settings_are_missing()
    {
        using var misconfigured = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(DatabaseSettings.DatabaseKey, string.Empty);
            builder.UseSetting(DatabaseSettings.PasswordKey, string.Empty);
        });

        var exception = Assert.Throws<InvalidOperationException>(() => misconfigured.CreateClient());

        Assert.Contains("Missing required configuration: POSTGRES_DB, POSTGRES_PASSWORD.", exception.Message, StringComparison.Ordinal);
    }
}
