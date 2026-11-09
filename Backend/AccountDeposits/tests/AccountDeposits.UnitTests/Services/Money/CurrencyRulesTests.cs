using AccountDeposits.Api.Services.Money;

namespace AccountDeposits.UnitTests.Services.Money;

public sealed class CurrencyRulesTests
{
    // The rates from Task.md section 4: 1 EUR = 1.10 USD = 0.85 GBP.
    private const decimal EurPerEur = 1m;
    private const decimal UsdPerEur = 1.10m;
    private const decimal GbpPerEur = 0.85m;

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

    public static TheoryData<decimal, decimal, decimal, decimal> ReceivedAmountCases => new()
    {
        // Section 4.4: amount sent, sender rate, receiver rate, amount received
        { 110.00m, UsdPerEur, EurPerEur, 100.00m },
        { 100.00m, UsdPerEur, GbpPerEur, 77.27m },
        { 10.01m, EurPerEur, UsdPerEur, 11.01m },
        { 50.00m, EurPerEur, EurPerEur, 50.00m },
    };

    public static TheoryData<decimal, decimal, decimal> MinimumTransferCases => new()
    {
        // sender rate, receiver rate, minimum amount in the sender's currency
        { UsdPerEur, GbpPerEur, 0.02m },
        { UsdPerEur, EurPerEur, 0.02m },
        { EurPerEur, UsdPerEur, 0.01m },
        { GbpPerEur, EurPerEur, 0.01m },
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
    [MemberData(nameof(ReceivedAmountCases))]
    public void ReceivedAmount_reproduces_the_transfer_examples(
        decimal amount, decimal senderRatePerEur, decimal receiverRatePerEur, decimal expected) =>
        Assert.Equal(expected, CurrencyRules.ReceivedAmount(amount, senderRatePerEur, receiverRatePerEur));

    [Theory]
    [MemberData(nameof(MinimumTransferCases))]
    public void MinimumTransferAmount_is_the_smallest_amount_that_credits_one_cent(
        decimal senderRatePerEur, decimal receiverRatePerEur, decimal expected)
    {
        var minimum = CurrencyRules.MinimumTransferAmount(senderRatePerEur, receiverRatePerEur);

        Assert.Equal(expected, minimum);
        Assert.True(CurrencyRules.ReceivedAmount(minimum, senderRatePerEur, receiverRatePerEur) >= 0.01m);
        Assert.Equal(0.00m, CurrencyRules.ReceivedAmount(minimum - 0.01m, senderRatePerEur, receiverRatePerEur));
    }

    [Theory]
    [MemberData(nameof(IsValidAmountCases))]
    public void IsValidAmount_accepts_positive_whole_cents(decimal amount, bool expected) =>
        Assert.Equal(expected, CurrencyRules.IsValidAmount(amount));
}
