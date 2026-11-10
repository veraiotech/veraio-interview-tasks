using System.Net;
using AccountDeposits.Api.Dtos;
using AccountDeposits.Api.Models;

namespace AccountDeposits.E2ETests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class TransferEndpointTests(AccountDepositsApiFactory factory) : ApiTestBase(factory)
{
    public static TheoryData<long, long, decimal, decimal, decimal, Currency> TransferExamples => new()
    {
        // Task.md section 4.4: from a basic account, to a premium account, amount, fee, received, received currency
        { SeedData.BasicUsd, SeedData.PremiumEur, 110.00m, 1.10m, 100.00m, Currency.EUR },
        { SeedData.BasicUsd, SeedData.PremiumGbp, 100.00m, 1.10m, 77.27m, Currency.GBP },
        { SeedData.BasicEur, SeedData.PremiumUsd, 10.01m, 1.00m, 11.01m, Currency.USD },
    };

    [Theory]
    [MemberData(nameof(TransferExamples))]
    public async Task Reproduces_the_transfer_examples(
        long fromAccountId, long toAccountId, decimal amount, decimal fee, decimal received, Currency receivedCurrency)
    {
        var result = await ReadAsync<TransferResponse>(await TransferAsync(SeedData.BasicUser, fromAccountId, toAccountId, amount));

        Assert.Equal(fee, result.Fee);
        Assert.Equal(SeedData.StartingBalance - amount - fee, result.BalanceAfter);
        Assert.Equal(received, result.ReceivedAmount);
        Assert.Equal(receivedCurrency, result.ReceivedCurrency);
        Assert.Equal(SeedData.StartingBalance + received, await GetBalanceAsync(SeedData.PremiumUser, toAccountId));
    }

    [Fact]
    public async Task Both_sides_are_recorded_under_one_reference()
    {
        var result = await ReadAsync<TransferResponse>(await TransferAsync(SeedData.BasicUser, SeedData.BasicUsd, SeedData.PremiumGbp, 100.00m));

        var operations = await GetOperationsAsync();
        Assert.Collection(
            operations,
            sent => Assert.Equal((result.Reference, SeedData.BasicUsd, OperationType.TransferOut, 100.00m, 1.10m), (sent.Reference, sent.AccountId, sent.Type, sent.Amount, sent.Fee)),
            received => Assert.Equal((result.Reference, SeedData.PremiumGbp, OperationType.TransferIn, 77.27m, 0.00m), (received.Reference, received.AccountId, received.Type, received.Amount, received.Fee)));
    }

    [Theory]
    [InlineData(SeedData.BasicUsd)]
    [InlineData(SeedData.BasicEur)]
    public async Task Transfer_to_the_same_user_is_refused(long toAccountId)
    {
        await AssertErrorAsync(TransferAsync(SeedData.BasicUser, SeedData.BasicEur, toAccountId, 10.00m), HttpStatusCode.UnprocessableEntity, "SELF_TRANSFER");
    }

    [Fact]
    public async Task Transfer_crediting_less_than_a_cent_is_refused_with_the_minimum_amount()
    {
        var error = await AssertErrorAsync(
            TransferAsync(SeedData.BasicUser, SeedData.BasicUsd, SeedData.PremiumGbp, 0.01m), HttpStatusCode.UnprocessableEntity, "TRANSFER_AMOUNT_TOO_SMALL");
        Assert.Contains("0.02 USD", error.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Transfer_of_exactly_the_minimum_amount_is_accepted()
    {
        var result = await ReadAsync<TransferResponse>(await TransferAsync(SeedData.BasicUser, SeedData.BasicUsd, SeedData.PremiumGbp, 0.02m));

        Assert.Equal(0.01m, result.ReceivedAmount);
        Assert.Equal(Currency.GBP, result.ReceivedCurrency);
    }

    [Fact]
    public async Task Amount_plus_fee_above_the_balance_is_refused_on_both_sides()
    {
        await AssertErrorAsync(TransferAsync(SeedData.LowBalanceUser, SeedData.LowBalanceEur, SeedData.PremiumEur, 100.00m), HttpStatusCode.UnprocessableEntity, "INSUFFICIENT_FUNDS");
        Assert.Equal(100.00m, await GetBalanceAsync(SeedData.LowBalanceUser, SeedData.LowBalanceEur));
        Assert.Equal(SeedData.StartingBalance, await GetBalanceAsync(SeedData.PremiumUser, SeedData.PremiumEur));
        Assert.Empty(await GetOperationsAsync());
    }

    [Fact]
    public async Task Cross_currency_transfer_is_refused_without_rates()
    {
        Factory.Rates.IsAvailable = false;

        await AssertErrorAsync(TransferAsync(SeedData.BasicUser, SeedData.BasicUsd, SeedData.PremiumGbp, 100.00m), HttpStatusCode.ServiceUnavailable, "RATES_UNAVAILABLE");
        Assert.Equal(SeedData.StartingBalance, await GetBalanceAsync(SeedData.BasicUser, SeedData.BasicUsd));
        Assert.Equal(SeedData.StartingBalance, await GetBalanceAsync(SeedData.PremiumUser, SeedData.PremiumGbp));
        Assert.Empty(await GetOperationsAsync());
    }

    [Fact]
    public async Task Eur_transfer_works_without_rates()
    {
        Factory.Rates.IsAvailable = false;

        var result = await ReadAsync<TransferResponse>(await TransferAsync(SeedData.BasicUser, SeedData.BasicEur, SeedData.PremiumEur, 100.00m));

        Assert.Equal(100.00m, result.ReceivedAmount);
    }

    [Fact]
    public async Task Unknown_receiver_account_is_not_found()
    {
        await AssertErrorAsync(TransferAsync(SeedData.BasicUser, SeedData.BasicEur, 999, 10.00m), HttpStatusCode.NotFound, "RECEIVER_ACCOUNT_NOT_FOUND");
    }
}
