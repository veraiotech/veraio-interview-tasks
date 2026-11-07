using System.Text.Json;
using AccountDeposits.Api.Errors;

namespace AccountDeposits.Api.Services.Money;

public sealed class FrankfurterExchangeRateProvider(
    HttpClient httpClient,
    ILogger<FrankfurterExchangeRateProvider> logger) : IExchangeRateProvider
{
    public static readonly Uri BaseAddress = new("https://api.frankfurter.dev/");

    private const string LatestRatesPath = "v1/latest?base=EUR&symbols=USD,GBP";

    public async Task<ExchangeRates> GetRatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<FrankfurterResponse>(LatestRatesPath, cancellationToken);

            if (response?.Rates is { } rates && rates.TryGetValue("USD", out var usd) && rates.TryGetValue("GBP", out var gbp))
            {
                return new ExchangeRates(usd, gbp);
            }

            logger.LogWarning("Frankfurter returned no usable USD and GBP rates.");
        }
        catch (Exception exception) when (
            exception is HttpRequestException or JsonException
            || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning(exception, "Could not get exchange rates from Frankfurter.");
        }

        throw new ApiException(
            StatusCodes.Status503ServiceUnavailable, "RATES_UNAVAILABLE", "Exchange rates are unavailable. Try again later.");
    }

    private sealed record FrankfurterResponse(Dictionary<string, decimal>? Rates);
}
