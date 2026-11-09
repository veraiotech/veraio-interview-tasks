namespace AccountDeposits.Api.Services.Money;

/// <summary>Rounding and precision rules. EUR, USD and GBP all have a smallest unit of 0.01.</summary>
public static class CurrencyRules
{
    private const int Decimals = 2;
    private const decimal SmallestUnit = 0.01m;

    // Dividing and multiplying by a rate leaves noise far past this scale, such as 0.50000000000000000000000001.
    // It is removed first, so that rounding up cannot turn 0.50 into 0.51.
    private const int NoiseFreeDecimals = 10;

    /// <summary>Rounds a fee up, so the service never undercharges.</summary>
    public static decimal RoundUp(decimal amount) =>
        Math.Round(Math.Round(amount, NoiseFreeDecimals), Decimals, MidpointRounding.ToPositiveInfinity);

    /// <summary>Rounds a transfer credit down, so the receiver is never overpaid...</summary>
    public static decimal RoundDown(decimal amount) =>
        Math.Round(Math.Round(amount, NoiseFreeDecimals), Decimals, MidpointRounding.ToNegativeInfinity);

    /// <summary>The smallest amount in the sender's currency that credits the receiver at least one cent.</summary>
    public static decimal MinimumTransferAmount(decimal senderRatePerEur, decimal receiverRatePerEur) =>
        RoundUp(SmallestUnit * senderRatePerEur / receiverRatePerEur);

    public static bool IsValidAmount(decimal amount) =>
        amount > 0 && amount == Math.Round(amount, Decimals);
}
