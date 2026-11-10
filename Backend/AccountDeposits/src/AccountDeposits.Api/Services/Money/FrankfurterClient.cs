using System.Text.Json;

namespace AccountDeposits.Api.Services.Money;

/// <summary>Calls Frankfurter for the latest rates. Only <see cref="ExchangeRateRefresher"/> uses it.</summary>
public sealed class FrankfurterClient(
    HttpClient httpClient,
    ILogger<FrankfurterClient> logger)
{
    public static readonly Uri BaseAddress = new("https://api.frankfurter.dev/");

    private const string LatestRatesPath = "v1/latest?base=EUR&symbols=USD,GBP";

    /// <summary>
    /// The latest rates and the day they were published for, or null when Frankfurter is unavailable or returns
    /// incomplete rates. The reason is logged here.
    /// </summary>
    public async Task<(ExchangeRates Rates, DateOnly Date)?> GetLatestAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<FrankfurterResponse>(LatestRatesPath, cancellationToken);

            // Only a complete set of positive rates counts, so a broken response never replaces good rates.
            if (response is { Date: { } date, Rates: { } rates }
                && rates.TryGetValue("USD", out var usd) && usd > 0
                && rates.TryGetValue("GBP", out var gbp) && gbp > 0)
            {
                return (new ExchangeRates(usd, gbp), date);
            }

            logger.LogWarning("Frankfurter returned no usable date, USD and GBP rates.");
        }
        catch (Exception exception) when (
            exception is HttpRequestException or JsonException
            || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning(exception, "Could not get exchange rates from Frankfurter.");
        }

        return null;
    }

    private sealed record FrankfurterResponse(DateOnly? Date, Dictionary<string, decimal>? Rates);
}
