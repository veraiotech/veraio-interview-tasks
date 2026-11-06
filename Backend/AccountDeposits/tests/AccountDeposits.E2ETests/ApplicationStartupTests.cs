using System.Net;
using AccountDeposits.Api.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace AccountDeposits.E2ETests;

public sealed class ApplicationStartupTests(AccountDepositsApiFactory factory)
    : IClassFixture<AccountDepositsApiFactory>
{
    [Fact]
    public async Task Application_starts_and_serves_openapi_document()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

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
