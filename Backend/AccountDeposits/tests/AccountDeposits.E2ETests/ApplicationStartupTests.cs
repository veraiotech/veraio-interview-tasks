using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AccountDeposits.E2ETests;

public sealed class ApplicationStartupTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Application_starts_and_serves_openapi_document()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
