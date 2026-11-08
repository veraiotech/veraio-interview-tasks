using AccountDeposits.Api.Services.Fees;

namespace AccountDeposits.UnitTests.Services.Fees;

/// <summary>Uses the rates from Task.md section 4: 1 EUR = 1.10 USD = 0.85 GBP.</summary>
public sealed class FeeCalculatorTests
{
    private const decimal EurPerEur = 1m;
    private const decimal UsdPerEur = 1.10m;
    private const decimal GbpPerEur = 0.85m;

    public static TheoryData<decimal, decimal, decimal> DepositExamples => new()
    {
        { 1_000.00m, EurPerEur, 0.50m },
        { 10_000.00m, EurPerEur, 3.00m },
        { 1_000.00m, UsdPerEur, 0.50m },
        { 10_000.00m, GbpPerEur, 2.55m },
        { 10.00m, GbpPerEur, 0.01m },
    };

    [Theory]
    [MemberData(nameof(DepositExamples))]
    public void Deposit_reproduces_the_task_examples(decimal amount, decimal ratePerEur, decimal expectedFee) =>
        Assert.Equal(expectedFee, FeeCalculator.Deposit(amount, ratePerEur));
}
