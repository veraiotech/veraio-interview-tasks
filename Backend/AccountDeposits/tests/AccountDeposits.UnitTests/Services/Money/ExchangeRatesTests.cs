using AccountDeposits.Api.Models;
using AccountDeposits.Api.Services.Money;

namespace AccountDeposits.UnitTests.Services.Money;

public sealed class ExchangeRatesTests
{
    private readonly ExchangeRates _rates = new(usdPerEur: 1.10m, gbpPerEur: 0.85m);

    public static TheoryData<decimal, Currency, Currency, decimal> Transfers => new()
    {
        { 110.00m, Currency.USD, Currency.EUR, 100.00m },
        { 100.00m, Currency.USD, Currency.GBP, 77.27m },
        { 10.01m, Currency.EUR, Currency.USD, 11.01m },
        { 50.00m, Currency.EUR, Currency.EUR, 50.00m },
    };

    [Theory]
    [MemberData(nameof(Transfers))]
    public void Convert_reproduces_the_transfer_examples(decimal amount, Currency from, Currency to, decimal expected) =>
        Assert.Equal(expected, CurrencyRules.RoundDown(_rates.Convert(amount, from, to)));

    [Fact]
    public void ToEur_divides_by_the_rate() =>
        Assert.Equal(2500m, _rates.ToEur(2750m, Currency.USD));

    [Fact]
    public void FromEur_multiplies_by_the_rate() =>
        Assert.Equal(2.55m, _rates.FromEur(3m, Currency.GBP));

    [Fact]
    public void Eur_needs_no_conversion() =>
        Assert.Equal(1m, _rates.PerEur(Currency.EUR));
}
