using System.Net;
using AccountDeposits.Api.Dtos;
using AccountDeposits.Api.Models;

namespace AccountDeposits.E2ETests.Endpoints;

/// <summary>
/// Task.md section 5.4: operations made at the same moment can never overdraw an account or use the premium
/// allowance twice. Each test sends its requests all at once.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ConcurrencyTests(AccountDepositsApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Concurrent_deposits_into_one_account_are_all_kept()
    {
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => DepositAsync(SeedData.LowBalanceUser, SeedData.LowBalanceEur, 1.00m)));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));

        // Each 1.00 EUR deposit pays a 0.01 EUR fee.
        Assert.Equal(100.00m + 20 * 0.99m, await GetBalanceAsync(SeedData.LowBalanceUser, SeedData.LowBalanceEur));
        Assert.Equal(20, (await GetHistoryAsync(SeedData.LowBalanceUser)).Count);
    }

    [Fact]
    public async Task Concurrent_withdrawals_never_overdraw_an_account()
    {
        // Each withdrawal takes 10.00 plus a 1.00 fee, so the 100.00 balance covers exactly nine of the twenty.
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => WithdrawAsync(SeedData.LowBalanceUser, SeedData.LowBalanceEur, 10.00m)));

        Assert.Equal(9, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Equal(11, responses.Count(response => response.StatusCode == HttpStatusCode.UnprocessableEntity));
        Assert.Equal(1.00m, await GetBalanceAsync(SeedData.LowBalanceUser, SeedData.LowBalanceEur));
        Assert.Equal(9, (await GetHistoryAsync(SeedData.LowBalanceUser)).Count);
    }

    [Fact]
    public async Task Concurrent_premium_withdrawals_use_the_allowance_only_once()
    {
        var fees = await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ =>
            (await ReadAsync<OperationResponse>(await WithdrawAsync(SeedData.PremiumUser, SeedData.PremiumEur, 100.00m))).Fee));

        // Only the first five operations of the week are free, whichever requests they turn out to be.
        Assert.Equal(5, fees.Count(fee => fee == 0.00m));
        Assert.Equal(5, fees.Count(fee => fee == 0.25m));
    }

    [Fact]
    public async Task Concurrent_premium_withdrawals_from_different_accounts_share_one_allowance()
    {
        // 100.00 EUR and 110.00 USD are the same amount in EUR, so each operation uses the allowance equally.
        var fees = await Task.WhenAll(Enumerable.Range(0, 10).Select(async i =>
        {
            var response = i % 2 == 0
                ? await WithdrawAsync(SeedData.PremiumUser, SeedData.PremiumEur, 100.00m)
                : await WithdrawAsync(SeedData.PremiumUser, SeedData.PremiumUsd, 110.00m);
            return (await ReadAsync<OperationResponse>(response)).Fee;
        }));

        // Five operations in total get the allowance, from whichever account they come.
        Assert.Equal(5, fees.Count(fee => fee == 0.00m));
    }

    [Fact]
    public async Task Opposite_transfers_at_the_same_moment_neither_deadlock_nor_lose_money()
    {
        var responses = await Task.WhenAll(Enumerable.Range(0, 20).SelectMany(_ => new[]
        {
            TransferAsync(SeedData.BasicUser, SeedData.BasicEur, SeedData.PremiumEur, 10.00m),
            TransferAsync(SeedData.PremiumUser, SeedData.PremiumEur, SeedData.BasicEur, 10.00m),
        }));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));

        // The amounts cancel out, so the two accounts together lose exactly the fees.
        var operations = await GetOperationsAsync();
        Assert.Equal(80, operations.Count);
        Assert.Equal(
            2 * SeedData.StartingBalance - operations.Sum(operation => operation.Fee),
            await GetBalanceAsync(SeedData.BasicUser, SeedData.BasicEur) + await GetBalanceAsync(SeedData.PremiumUser, SeedData.PremiumEur));

        // Each balance is exactly what its own history adds up to.
        foreach (var (userId, accountId) in new[] { (SeedData.BasicUser, SeedData.BasicEur), (SeedData.PremiumUser, SeedData.PremiumEur) })
        {
            var expected = SeedData.StartingBalance + operations
                .Where(operation => operation.AccountId == accountId)
                .Sum(operation => operation.Type == OperationType.TransferIn ? operation.Amount : -(operation.Amount + operation.Fee));
            Assert.Equal(expected, await GetBalanceAsync(userId, accountId));
        }
    }
}
