using AccountDeposits.Api.Errors;
using AccountDeposits.Api.Models;
using AccountDeposits.Api.Services.Money;
using Microsoft.Extensions.Time.Testing;

namespace AccountDeposits.UnitTests.Services.Money;

public sealed class ExchangeRateStoreTests
{
    private static readonly ExchangeRates Rates = new(1.10m, 0.85m);

    private static readonly DateOnly Today = new(2026, 11, 9);

    private readonly ExchangeRateStore _store =
        new(new FakeTimeProvider(new DateTimeOffset(2026, 11, 9, 10, 0, 0, TimeSpan.Zero)));

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Rates_are_unavailable_until_the_first_refresh() =>
        await AssertUnavailableAsync(_store);

    [Fact]
    public async Task Rates_published_up_to_five_days_ago_are_used()
    {
        _store.Update(Rates, Today.AddDays(-ExchangeRateStore.MaxAgeInDays));

        var rates = await _store.GetRatesAsync(CancellationToken);

        Assert.Equal(1.10m, rates.PerEur(Currency.USD));
    }

    [Fact]
    public async Task Rates_published_more_than_five_days_ago_are_unavailable()
    {
        // Also what happens when Frankfurter keeps answering with old rates.
        _store.Update(Rates, Today.AddDays(-ExchangeRateStore.MaxAgeInDays - 1));

        await AssertUnavailableAsync(_store);
    }

    [Fact]
    public async Task Easter_without_new_rates_from_thursday_to_tuesday_still_works()
    {
        // In 2027 the ECB publishes on Thursday 25 March and next on Tuesday 30 March, at about 15:00 UTC.
        var store = new ExchangeRateStore(new FakeTimeProvider(new DateTimeOffset(2027, 3, 30, 10, 0, 0, TimeSpan.Zero)));
        store.Update(Rates, new DateOnly(2027, 3, 25));

        var rates = await store.GetRatesAsync(CancellationToken);

        Assert.Equal(1.10m, rates.PerEur(Currency.USD));
    }

    private static async Task AssertUnavailableAsync(ExchangeRateStore store)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(() => store.GetRatesAsync(CancellationToken));
        Assert.Equal("RATES_UNAVAILABLE", exception.Code);
    }
}
