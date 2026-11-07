namespace AccountDeposits.Api.Services.Money;

public interface IExchangeRateProvider
{
    /// <summary>Gets the day's rates. Throws when they are unavailable.</summary>
    Task<ExchangeRates> GetRatesAsync(CancellationToken cancellationToken);
}
