using AccountDeposits.Api.Models;

namespace AccountDeposits.Api.Services.Money;

/// <summary>The day's rates, as units of each currency for 1 EUR. Every conversion goes through EUR.</summary>
public sealed class ExchangeRates(decimal usdPerEur, decimal gbpPerEur)
{
    public decimal PerEur(Currency currency) => currency switch
    {
        Currency.EUR => 1m,
        Currency.USD => usdPerEur,
        Currency.GBP => gbpPerEur,
        _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Unsupported currency."),
    };

    public decimal ToEur(decimal amount, Currency currency) => amount / PerEur(currency);

    public decimal FromEur(decimal amountInEur, Currency currency) => amountInEur * PerEur(currency);

    /// <summary>Multiplies before dividing, so the single division adds as little rounding noise as possible.</summary>
    public decimal Convert(decimal amount, Currency from, Currency to) => amount * PerEur(to) / PerEur(from);
}
