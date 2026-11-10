using System.Net;
using AccountDeposits.Api.Dtos;
using AccountDeposits.Api.Models;

namespace AccountDeposits.E2ETests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class HistoryEndpointTests(AccountDepositsApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task A_user_without_operations_has_an_empty_history() =>
        Assert.Empty(await GetHistoryAsync(SeedData.BasicUser));

    [Fact]
    public async Task Unknown_user_is_not_found() =>
        await AssertErrorAsync(Client.GetAsync("users/999/operations", CancellationToken), HttpStatusCode.NotFound, "USER_NOT_FOUND");

    [Fact]
    public async Task Lists_every_operation_of_the_user_newest_first()
    {
        var start = Factory.Clock.GetUtcNow();
        var deposit = await ReadAsync<OperationResponse>(await DepositAsync(SeedData.BasicUser, SeedData.BasicEur, 1_000.00m));
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        var withdrawal = await ReadAsync<OperationResponse>(await WithdrawAsync(SeedData.BasicUser, SeedData.BasicEur, 100.00m));
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        var transfer = await ReadAsync<TransferResponse>(
            await TransferAsync(SeedData.BasicUser, SeedData.BasicUsd, SeedData.PremiumGbp, 100.00m));

        var history = await GetHistoryAsync(SeedData.BasicUser);

        Assert.Equal(
            [
                new OperationHistoryItem(
                    transfer.Reference, OperationType.TransferOut, SeedData.BasicUsd, 100.00m, Currency.USD, 1.10m,
                    SeedData.StartingBalance - 101.10m, start.AddMinutes(2), SeedData.PremiumGbp),
                new OperationHistoryItem(
                    withdrawal.Reference, OperationType.Withdrawal, SeedData.BasicEur, 100.00m, Currency.EUR, 1.00m,
                    SeedData.StartingBalance + 999.50m - 101.00m, start.AddMinutes(1), null),
                new OperationHistoryItem(
                    deposit.Reference, OperationType.Deposit, SeedData.BasicEur, 1_000.00m, Currency.EUR, 0.50m,
                    SeedData.StartingBalance + 999.50m, start, null),
            ],
            history);
    }

    [Fact]
    public async Task The_receiver_sees_the_incoming_side_of_a_transfer()
    {
        var transfer = await ReadAsync<TransferResponse>(
            await TransferAsync(SeedData.BasicUser, SeedData.BasicUsd, SeedData.PremiumGbp, 100.00m));

        var history = await GetHistoryAsync(SeedData.PremiumUser);

        Assert.Equal(
            [
                new OperationHistoryItem(
                    transfer.Reference, OperationType.TransferIn, SeedData.PremiumGbp, 77.27m, Currency.GBP, 0.00m,
                    SeedData.StartingBalance + 77.27m, Factory.Clock.GetUtcNow(), SeedData.BasicUsd),
            ],
            history);
    }

    [Fact]
    public async Task A_refused_operation_does_not_appear_in_the_history()
    {
        await AssertErrorAsync(
            WithdrawAsync(SeedData.LowBalanceUser, SeedData.LowBalanceEur, 99.50m),
            HttpStatusCode.UnprocessableEntity,
            "INSUFFICIENT_FUNDS");

        Assert.Empty(await GetHistoryAsync(SeedData.LowBalanceUser));
    }

    [Fact]
    public async Task Operations_at_the_same_moment_are_listed_newest_first()
    {
        var first = await ReadAsync<OperationResponse>(await DepositAsync(SeedData.BasicUser, SeedData.BasicEur, 10.00m));
        var second = await ReadAsync<OperationResponse>(await DepositAsync(SeedData.BasicUser, SeedData.BasicEur, 20.00m));

        var history = await GetHistoryAsync(SeedData.BasicUser);

        Assert.Equal([second.Reference, first.Reference], history.Select(item => item.Reference));
    }
}
