using AccountDeposits.Api.Services.Money;

namespace AccountDeposits.Api.Services.Fees;

/// <summary>
/// Converts the EUR fee limits into the account currency instead of converting the amount into EUR.
/// The result is the same, but avoids division, so no rounding noise appears.
/// <c>ratePerEur</c> is how many units of the account currency make 1 EUR, so it is 1 for EUR accounts.
/// </summary>
public static class FeeCalculator
{
    private const decimal DepositFeePercent = 0.05m;
    private const decimal DepositFeeCapInEur = 3.00m;

    public static decimal Deposit(decimal amount, decimal ratePerEur)
    {
        var fee = amount * DepositFeePercent / 100;
        var cap = DepositFeeCapInEur * ratePerEur;

        return CurrencyRules.RoundUp(Math.Min(fee, cap));
    }
}
