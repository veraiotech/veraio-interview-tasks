using System.Net;
using AccountDeposits.Api.Dtos;
using AccountDeposits.Api.Models;

namespace AccountDeposits.E2ETests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class AccountsEndpointTests(AccountDepositsApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Returns_every_account_of_the_user()
    {
        var accounts = await GetAccountsAsync(SeedData.BasicUser);

        Assert.Equal(
            [
                new AccountResponse(SeedData.BasicEur, Currency.EUR, SeedData.StartingBalance),
                new AccountResponse(SeedData.BasicUsd, Currency.USD, SeedData.StartingBalance),
                new AccountResponse(SeedData.BasicGbp, Currency.GBP, SeedData.StartingBalance),
            ],
            accounts);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Accounts_cannot_be_created_changed_or_removed(string method)
    {
        // Task.md section 5.1: users and accounts are predefined.
        using var request = new HttpRequestMessage(new HttpMethod(method), $"users/{SeedData.BasicUser}/accounts");

        await AssertErrorAsync(Client.SendAsync(request, CancellationToken), HttpStatusCode.MethodNotAllowed, "METHOD_NOT_ALLOWED");
    }

    [Fact]
    public async Task Unknown_user_is_not_found()
    {
        await AssertErrorAsync(Client.GetAsync("users/999/accounts", CancellationToken), HttpStatusCode.NotFound, "USER_NOT_FOUND");
    }
}
