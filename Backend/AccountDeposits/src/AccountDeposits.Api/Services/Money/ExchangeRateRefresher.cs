namespace AccountDeposits.Api.Services.Money;

/// <summary>
/// Fetches the rates at startup and then once a day, after the European Central Bank publishes them, retrying
/// soon after a failure or when Frankfurter does not have the day's rates yet. It is a single loop, so two
/// refreshes never run at the same time. Until the first refresh succeeds the service still runs, but operations
/// that need a conversion are refused.
/// </summary>
public sealed class ExchangeRateRefresher(
    IServiceScopeFactory scopeFactory,
    ExchangeRateStore store,
    TimeProvider timeProvider,
    ILogger<ExchangeRateRefresher> logger) : BackgroundService
{
    /// <summary>Refreshes at 16:00 UTC, allowing time for the ECB reference rates to become available through Frankfurter.</summary>
    public static readonly TimeSpan DailyRefreshTime = TimeSpan.FromHours(16);

    public static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(5);

    /// <summary>The next daily refresh: today at <see cref="DailyRefreshTime"/> UTC if that is still ahead, otherwise tomorrow.</summary>
    public static DateTimeOffset NextDailyRefresh(DateTimeOffset now)
    {
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero).Add(DailyRefreshTime);
        return now < today ? today : today.AddDays(1);
    }

    /// <summary>
    /// Whether the rates are the newest ones to expect: today's, or older on a weekend, when the ECB publishes
    /// nothing, or before <see cref="DailyRefreshTime"/>, when today's are not out yet. On an ECB holiday that falls
    /// on a weekday this is false after 16:00 UTC, so the service retries until midnight; that is expected.
    /// </summary>
    public static bool IsCurrent(DateOnly ratesDate, DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var isWeekend = today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var isBeforeTodaysPublication = now.UtcDateTime.TimeOfDay < DailyRefreshTime;

        return ratesDate == today || isWeekend || isBeforeTodaysPublication;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Lets the host finish starting before the first call to Frankfurter.
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = RetryDelay;

            if (await TryRefreshAsync(stoppingToken))
            {
                // One reading of the clock, so the delay can never come out negative around 16:00.
                var now = timeProvider.GetUtcNow();
                delay = NextDailyRefresh(now) - now;
            }

            await Task.Delay(delay, timeProvider, stoppingToken);
        }
    }

    /// <summary>True when the stored rates are current, so the next refresh can wait for tomorrow.</summary>
    private async Task<bool> TryRefreshAsync(CancellationToken stoppingToken)
    {
        try
        {
            // A typed HttpClient is short-lived by design, so a new one is taken for every refresh.
            using var scope = scopeFactory.CreateScope();
            var frankfurter = scope.ServiceProvider.GetRequiredService<FrankfurterClient>();

            if (await frankfurter.GetLatestAsync(stoppingToken) is not { } latest)
            {
                // The client has already logged why.
                logger.LogInformation("Exchange rates were not refreshed. Retrying in {RetryDelay}.", RetryDelay);
                return false;
            }

            store.Update(latest.Rates, latest.Date);

            if (IsCurrent(latest.Date, timeProvider.GetUtcNow()))
            {
                logger.LogInformation("Exchange rates for {Date} refreshed.", latest.Date);
                return true;
            }

            logger.LogInformation(
                "Frankfurter still has the rates for {Date}. Retrying in {RetryDelay}.", latest.Date, RetryDelay);
            return false;
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            // Anything unexpected is logged here, so the loop keeps running instead of stopping the service.
            logger.LogError(exception, "Exchange rates refresh failed. Retrying in {RetryDelay}.", RetryDelay);
            return false;
        }
    }
}
