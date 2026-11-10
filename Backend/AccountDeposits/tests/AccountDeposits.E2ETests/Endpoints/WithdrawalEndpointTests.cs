using System.Net;
using AccountDeposits.Api.Dtos;

namespace AccountDeposits.E2ETests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class WithdrawalEndpointTests(AccountDepositsApiFactory factory) : ApiTestBase(factory)
{
    public static TheoryData<long, decimal, decimal> BasicWithdrawalExamples => new()
    {
        // Task.md section 4.2: account of the basic user, amount, fee
        { SeedData.BasicEur, 100.00m, 1.00m },
        { SeedData.BasicEur, 1_000.00m, 5.00m },
        { SeedData.BasicUsd, 333.00m, 1.67m },
        { SeedData.BasicGbp, 50.00m, 0.85m },
    };

    [Theory]
    [MemberData(nameof(BasicWithdrawalExamples))]
    public async Task Reproduces_the_basic_withdrawal_examples(long accountId, decimal amount, decimal fee)
    {
        var result = await ReadAsync<OperationResponse>(await WithdrawAsync(SeedData.BasicUser, accountId, amount));

        Assert.Equal(fee, result.Fee);
        Assert.Equal(SeedData.StartingBalance - amount - fee, result.BalanceAfter);
    }

    [Fact]
    public async Task Amount_plus_fee_above_the_balance_is_refused_and_not_recorded()
    {
        await AssertErrorAsync(WithdrawAsync(SeedData.LowBalanceUser, SeedData.LowBalanceEur, 99.50m), HttpStatusCode.UnprocessableEntity, "INSUFFICIENT_FUNDS");
        Assert.Equal(100.00m, await GetBalanceAsync(SeedData.LowBalanceUser, SeedData.LowBalanceEur));
        Assert.Empty(await GetOperationsAsync());
    }

    [Fact]
    public async Task Amount_plus_fee_equal_to_the_balance_is_accepted()
    {
        var result = await ReadAsync<OperationResponse>(await WithdrawAsync(SeedData.LowBalanceUser, SeedData.LowBalanceEur, 99.00m));

        Assert.Equal(1.00m, result.Fee);
        Assert.Equal(0.00m, result.BalanceAfter);
    }
}
