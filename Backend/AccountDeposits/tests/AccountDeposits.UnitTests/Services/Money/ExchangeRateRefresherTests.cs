using AccountDeposits.Api.Services.Money;

namespace AccountDeposits.UnitTests.Services.Money;

public sealed class ExchangeRateRefresherTests
{
    public static TheoryData<DateTimeOffset, DateTimeOffset> DailyRefreshCases => new()
    {
        // now, next daily refresh
        { new DateTimeOffset(2026, 11, 9, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 11, 9, 16, 0, 0, TimeSpan.Zero) },
        { new DateTimeOffset(2026, 11, 9, 16, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 11, 10, 16, 0, 0, TimeSpan.Zero) },
        { new DateTimeOffset(2026, 11, 9, 23, 30, 0, TimeSpan.Zero), new DateTimeOffset(2026, 11, 10, 16, 0, 0, TimeSpan.Zero) },
    };

    public static TheoryData<DateOnly, DateTimeOffset, bool> CurrentRatesCases => new()
    {
        // rates date, now, current
        { new DateOnly(2026, 11, 9), new DateTimeOffset(2026, 11, 9, 17, 0, 0, TimeSpan.Zero), true },   // today's rates
        { new DateOnly(2026, 11, 6), new DateTimeOffset(2026, 11, 7, 17, 0, 0, TimeSpan.Zero), true },   // Saturday, Friday's rates
        { new DateOnly(2026, 11, 6), new DateTimeOffset(2026, 11, 9, 10, 0, 0, TimeSpan.Zero), true },   // Monday morning, not published yet
        { new DateOnly(2026, 11, 6), new DateTimeOffset(2026, 11, 9, 17, 0, 0, TimeSpan.Zero), false },  // Monday evening, Frankfurter is late
    };

    [Theory]
    [MemberData(nameof(DailyRefreshCases))]
    public void Next_daily_refresh_is_the_next_16_00_utc(DateTimeOffset now, DateTimeOffset expected) =>
        Assert.Equal(expected, ExchangeRateRefresher.NextDailyRefresh(now));

    [Theory]
    [MemberData(nameof(CurrentRatesCases))]
    public void Rates_are_current_unless_a_weekday_publication_is_missing(DateOnly ratesDate, DateTimeOffset now, bool expected) =>
        Assert.Equal(expected, ExchangeRateRefresher.IsCurrent(ratesDate, now));
}
