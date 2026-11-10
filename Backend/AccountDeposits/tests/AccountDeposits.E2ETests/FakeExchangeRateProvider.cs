using AccountDeposits.Api.Errors;
using AccountDeposits.Api.Services.Money;
using Microsoft.AspNetCore.Http;

namespace AccountDeposits.E2ETests;

/// <summary>Returns the rates from Task.md section 4: 1 EUR = 1.10 USD = 0.85 GBP.</summary>
public sealed class FakeExchangeRateProvider : IExchangeRateProvider
{
    public bool IsAvailable { get; set; } = true;

    public Task<ExchangeRates> GetRatesAsync(CancellationToken cancellationToken) =>
        IsAvailable
            ? Task.FromResult(new ExchangeRates(usdPerEur: 1.10m, gbpPerEur: 0.85m))
            : throw new ApiException(StatusCodes.Status503ServiceUnavailable, "RATES_UNAVAILABLE", "Exchange rates are unavailable.");
}
