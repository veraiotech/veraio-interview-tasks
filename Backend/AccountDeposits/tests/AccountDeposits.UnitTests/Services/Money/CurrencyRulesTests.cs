using AccountDeposits.Api.Services.Money;

namespace AccountDeposits.UnitTests.Services.Money;

public sealed class CurrencyRulesTests
{
    public static TheoryData<decimal, decimal> RoundUpCases => new()
    {
        { 1.661m, 1.67m },
        { 1.665m, 1.67m },
        { 0.005m, 0.01m },
        { 0.50m, 0.50m },
    };

    public static TheoryData<decimal, decimal> RoundDownCases => new()
    {
        { 77.2727m, 77.27m },
        { 11.011m, 11.01m },
        { 100.00m, 100.00m },
    };

    public static TheoryData<decimal, bool> IsValidAmountCases => new()
    {
        { 10.00m, true },
        { 0.01m, true },
        { 0m, false },
        { -5m, false },
        { 10.001m, false },
        { 10.005m, false },
    };

    [Theory]
    [MemberData(nameof(RoundUpCases))]
    public void RoundUp_rounds_to_the_next_cent(decimal amount, decimal expected) =>
        Assert.Equal(expected, CurrencyRules.RoundUp(amount));

    [Theory]
    [MemberData(nameof(RoundDownCases))]
    public void RoundDown_drops_fractions_of_a_cent(decimal amount, decimal expected) =>
        Assert.Equal(expected, CurrencyRules.RoundDown(amount));

    [Fact]
    public void RoundUp_ignores_noise_left_by_a_conversion()
    {
        var fee = 1000m / 1.10m * 0.0005m * 1.10m;

        Assert.Equal(0.50m, CurrencyRules.RoundUp(fee));
    }

    [Fact]
    public void RoundDown_ignores_noise_left_by_a_conversion()
    {
        var credited = 1m / 3m * 3m;

        Assert.Equal(1.00m, CurrencyRules.RoundDown(credited));
    }

    [Theory]
    [MemberData(nameof(IsValidAmountCases))]
    public void IsValidAmount_accepts_positive_whole_cents(decimal amount, bool expected) =>
        Assert.Equal(expected, CurrencyRules.IsValidAmount(amount));
}
