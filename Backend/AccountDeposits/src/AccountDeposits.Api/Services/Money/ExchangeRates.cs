using AccountDeposits.Api.Models;

namespace AccountDeposits.Api.Services.Money;

/// <summary>The day's rates, as units of each currency for 1 EUR.</summary>
public sealed class ExchangeRates(decimal usdPerEur, decimal gbpPerEur)
{
    public decimal PerEur(Currency currency) => currency switch
    {
        Currency.EUR => 1m,
        Currency.USD => usdPerEur,
        Currency.GBP => gbpPerEur,
        _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Unsupported currency."),
    };
}
