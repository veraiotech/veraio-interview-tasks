using System.Net;
using System.Text;
using AccountDeposits.Api.Dtos;

namespace AccountDeposits.E2ETests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class DepositEndpointTests(AccountDepositsApiFactory factory) : ApiTestBase(factory)
{
    public static TheoryData<long, decimal, decimal> DepositExamples => new()
    {
        // Task.md section 4.1: account of the basic user, amount, fee
        { SeedData.BasicEur, 1_000.00m, 0.50m },
        { SeedData.BasicEur, 10_000.00m, 3.00m },
        { SeedData.BasicUsd, 1_000.00m, 0.50m },
        { SeedData.BasicGbp, 10_000.00m, 2.55m },
        { SeedData.BasicGbp, 10.00m, 0.01m },
    };

    public static TheoryData<decimal> InvalidAmounts => new() { 0m, -5m, 10.001m, 1_000_000_000.01m };

    [Theory]
    [MemberData(nameof(DepositExamples))]
    public async Task Reproduces_the_deposit_examples(long accountId, decimal amount, decimal fee)
    {
        var result = await ReadAsync<OperationResponse>(await DepositAsync(SeedData.BasicUser, accountId, amount));

        Assert.Equal(fee, result.Fee);
        Assert.Equal(SeedData.StartingBalance + amount - fee, result.BalanceAfter);
        Assert.Equal(result.BalanceAfter, await GetBalanceAsync(SeedData.BasicUser, accountId));
    }

    [Theory]
    [MemberData(nameof(InvalidAmounts))]
    public async Task Invalid_amount_is_refused_and_not_recorded(decimal amount)
    {
        await AssertErrorAsync(DepositAsync(SeedData.BasicUser, SeedData.BasicEur, amount), HttpStatusCode.BadRequest, "INVALID_AMOUNT");
        Assert.Empty(await GetOperationsAsync());
    }

    [Theory]
    [InlineData("""{ "amount": "abc" }""")]
    [InlineData("")]
    public async Task Invalid_json_returns_the_shared_error_format(string body)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        var error = await AssertErrorAsync(
            Client.PostAsync($"users/{SeedData.BasicUser}/accounts/{SeedData.BasicEur}/deposits", content, CancellationToken),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");

        Assert.Equal(400, error.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Account_of_another_user_is_not_found()
    {
        await AssertErrorAsync(DepositAsync(SeedData.BasicUser, SeedData.PremiumEur, 10.00m), HttpStatusCode.NotFound, "ACCOUNT_NOT_FOUND");
    }

    [Fact]
    public async Task Eur_deposit_works_without_rates_but_usd_deposit_is_refused()
    {
        Factory.Rates.IsAvailable = false;

        await ReadAsync<OperationResponse>(await DepositAsync(SeedData.BasicUser, SeedData.BasicEur, 10.00m));
        await AssertErrorAsync(DepositAsync(SeedData.BasicUser, SeedData.BasicUsd, 10.00m), HttpStatusCode.ServiceUnavailable, "RATES_UNAVAILABLE");
        Assert.Single(await GetOperationsAsync());
    }
}
