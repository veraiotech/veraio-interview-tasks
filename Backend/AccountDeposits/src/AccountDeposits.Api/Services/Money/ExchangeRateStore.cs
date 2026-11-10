using AccountDeposits.Api.Errors;

namespace AccountDeposits.Api.Services.Money;

/// <summary>
/// Keeps the last complete set of rates that <see cref="ExchangeRateRefresher"/> fetched, so operations never call
/// Frankfurter themselves. The rates count as unavailable until the first refresh succeeds, and again when the day
/// they were published for is more than <see cref="MaxAgeInDays"/> days ago.
/// </summary>
public sealed class ExchangeRateStore(TimeProvider timeProvider) : IExchangeRateProvider
{
    /// <summary>
    /// Covers weekends and holidays such as Easter, when the ECB publishes nothing from Thursday to Tuesday,
    /// but never lets operations use rates from long ago, whether Frankfurter is down or answers with old rates.
    /// </summary>
    public const int MaxAgeInDays = 5;

    // The rates and their date are replaced together, so a reader never sees one without the other.
    private volatile Snapshot? _latest;

    public void Update(ExchangeRates rates, DateOnly ratesDate) => _latest = new Snapshot(rates, ratesDate);

    public Task<ExchangeRates> GetRatesAsync(CancellationToken cancellationToken)
    {
        var latest = _latest;
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        if (latest is null || today.DayNumber - latest.RatesDate.DayNumber > MaxAgeInDays)
        {
            throw new ApiException(
                StatusCodes.Status503ServiceUnavailable, "RATES_UNAVAILABLE", "Exchange rates are unavailable. Try again later.");
        }

        return Task.FromResult(latest.Rates);
    }

    private sealed record Snapshot(ExchangeRates Rates, DateOnly RatesDate);
}
