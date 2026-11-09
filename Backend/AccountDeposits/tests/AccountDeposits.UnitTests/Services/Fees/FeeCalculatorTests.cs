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

    public static TheoryData<decimal, decimal, decimal> BasicOutgoingExamples => new()
    {
        // Section 4.2: withdrawals
        { 100.00m, EurPerEur, 1.00m },
        { 1_000.00m, EurPerEur, 5.00m },
        { 333.00m, UsdPerEur, 1.67m },
        { 50.00m, GbpPerEur, 0.85m },

        // Section 4.4: transfers
        { 110.00m, UsdPerEur, 1.10m },
        { 100.00m, UsdPerEur, 1.10m },
        { 10.01m, EurPerEur, 1.00m },
    };

    public static TheoryData<decimal, decimal, WeeklyUsage, decimal> PremiumOutgoingExamples => new()
    {
        // Section 4.3, scenario A: the allowance is used up by the amounts
        { 1_500.00m, EurPerEur, new WeeklyUsage(0, 0m), 0.00m },
        { 1_000.00m, EurPerEur, new WeeklyUsage(1, 1_500m), 1.25m },
        { 200.00m, EurPerEur, new WeeklyUsage(2, 2_500m), 0.50m },

        // Section 4.3, scenario B: the sixth operation pays on the full amount
        { 100.00m, EurPerEur, new WeeklyUsage(4, 400m), 0.00m },
        { 100.00m, EurPerEur, new WeeklyUsage(5, 500m), 0.25m },
        { 4_000.00m, EurPerEur, new WeeklyUsage(6, 600m), 10.00m },

        // Section 4.3, scenario C: a USD account
        { 2_750.00m, UsdPerEur, new WeeklyUsage(0, 0m), 1.38m },

        // The fifth operation still uses what is left of the allowance
        { 500.00m, EurPerEur, new WeeklyUsage(4, 1_800m), 0.75m },
    };

    [Theory]
    [MemberData(nameof(DepositExamples))]
    public void Deposit_reproduces_the_task_examples(decimal amount, decimal ratePerEur, decimal expectedFee) =>
        Assert.Equal(expectedFee, FeeCalculator.Deposit(amount, ratePerEur));

    [Theory]
    [MemberData(nameof(BasicOutgoingExamples))]
    public void BasicOutgoing_reproduces_the_task_examples(decimal amount, decimal ratePerEur, decimal expectedFee) =>
        Assert.Equal(expectedFee, FeeCalculator.BasicOutgoing(amount, ratePerEur));

    [Theory]
    [MemberData(nameof(PremiumOutgoingExamples))]
    public void PremiumOutgoing_reproduces_the_task_examples(
        decimal amount, decimal ratePerEur, WeeklyUsage usage, decimal expectedFee) =>
        Assert.Equal(expectedFee, FeeCalculator.PremiumOutgoing(amount, ratePerEur, usage));
}
