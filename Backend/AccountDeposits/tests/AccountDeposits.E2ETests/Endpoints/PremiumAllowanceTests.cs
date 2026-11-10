using System.Net;
using AccountDeposits.Api.Dtos;

namespace AccountDeposits.E2ETests.Endpoints;

/// <summary>
/// The premium weekly allowance from Task.md sections 4.3 and 5.3. It is shared by withdrawals and outgoing
/// transfers and by all accounts of the user, so these tests use several operations and accounts.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class PremiumAllowanceTests(AccountDepositsApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Scenario_A_the_allowance_is_used_up_by_the_amounts()
    {
        Assert.Equal(0.00m, await WithdrawalFeeAsync(1_500.00m));
        var transfer = await ReadAsync<TransferResponse>(await TransferAsync(SeedData.PremiumUser, SeedData.PremiumEur, SeedData.BasicEur, 1_000.00m));
        Assert.Equal(1.25m, transfer.Fee);
        Assert.Equal(0.50m, await WithdrawalFeeAsync(200.00m));
    }

    [Fact]
    public async Task Scenario_B_the_sixth_operation_pays_on_the_full_amount_until_next_monday()
    {
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(0.00m, await WithdrawalFeeAsync(100.00m));
        }

        Assert.Equal(0.25m, await WithdrawalFeeAsync(100.00m));
        Assert.Equal(10.00m, await WithdrawalFeeAsync(4_000.00m));

        // Sunday 23:59:59 is still the same week.
        Factory.Clock.SetUtcNow(new DateTimeOffset(2026, 11, 15, 23, 59, 59, TimeSpan.Zero));
        Assert.Equal(0.25m, await WithdrawalFeeAsync(100.00m));

        // Monday 00:00 starts a new week, with a new allowance and counter.
        Factory.Clock.SetUtcNow(new DateTimeOffset(2026, 11, 16, 0, 0, 0, TimeSpan.Zero));
        Assert.Equal(0.00m, await WithdrawalFeeAsync(100.00m));
    }

    [Fact]
    public async Task Scenario_C_a_usd_account()
    {
        var result = await ReadAsync<OperationResponse>(await WithdrawAsync(SeedData.PremiumUser, SeedData.PremiumUsd, 2_750.00m));

        Assert.Equal(1.38m, result.Fee);
    }

    [Fact]
    public async Task Refused_operation_uses_neither_allowance_nor_counter()
    {
        for (var i = 0; i < 4; i++)
        {
            await WithdrawalFeeAsync(100.00m);
        }

        await AssertErrorAsync(
            WithdrawAsync(SeedData.PremiumUser, SeedData.PremiumEur, 1_000_000.00m),
            HttpStatusCode.UnprocessableEntity,
            "INSUFFICIENT_FUNDS");

        Assert.Equal(0.00m, await WithdrawalFeeAsync(100.00m)); // still the fifth operation
    }

    [Fact]
    public async Task Deposits_and_incoming_transfers_use_neither_allowance_nor_counter()
    {
        for (var i = 0; i < 4; i++)
        {
            await ReadAsync<OperationResponse>(await DepositAsync(SeedData.PremiumUser, SeedData.PremiumEur, 1_000.00m));
        }

        await ReadAsync<TransferResponse>(await TransferAsync(SeedData.BasicUser, SeedData.BasicEur, SeedData.PremiumEur, 1_000.00m));

        Assert.Equal(0.00m, await WithdrawalFeeAsync(2_000.00m));
    }

    [Fact]
    public async Task The_allowance_is_shared_by_all_accounts_of_the_user()
    {
        Assert.Equal(0.00m, await WithdrawalFeeAsync(1_500.00m));

        // 500 EUR of allowance is left, that is 550 USD, so the fee is 0.25% of the other 550 USD.
        var result = await ReadAsync<OperationResponse>(await WithdrawAsync(SeedData.PremiumUser, SeedData.PremiumUsd, 1_100.00m));

        Assert.Equal(1.38m, result.Fee);
    }

    private async Task<decimal> WithdrawalFeeAsync(decimal amount) =>
        (await ReadAsync<OperationResponse>(await WithdrawAsync(SeedData.PremiumUser, SeedData.PremiumEur, amount))).Fee;
}
