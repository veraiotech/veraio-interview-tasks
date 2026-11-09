using AccountDeposits.Api.Services.Money;

namespace AccountDeposits.Api.Services.Fees;

/// <summary>
/// Calculates fees in the paying account's currency. EUR limits such as the deposit cap, the basic minimum and
/// the premium allowance are converted into that currency, so the amount itself is never divided by a rate.
/// <c>ratePerEur</c> is how many units of the account currency make 1 EUR, so it is 1 for EUR accounts.
/// </summary>
public static class FeeCalculator
{
    private const decimal DepositFeePercent = 0.05m;
    private const decimal DepositFeeCapInEur = 3.00m;
    private const decimal BasicFeePercent = 0.5m;
    private const decimal BasicFeeMinimumInEur = 1.00m;
    private const decimal PremiumFeePercent = 0.25m;
    private const decimal WeeklyAllowanceInEur = 2_000.00m;
    private const int OperationsWithAllowancePerWeek = 5;

    /// <summary>The fee for a deposit, the same for basic and premium users.</summary>
    public static decimal Deposit(decimal amount, decimal ratePerEur)
    {
        var fee = amount * DepositFeePercent / 100;
        var cap = DepositFeeCapInEur * ratePerEur;

        return CurrencyRules.RoundUp(Math.Min(fee, cap));
    }

    /// <summary>The fee a basic user pays for a withdrawal or an outgoing transfer.</summary>
    public static decimal BasicOutgoing(decimal amount, decimal ratePerEur)
    {
        var fee = amount * BasicFeePercent / 100;
        var minimum = BasicFeeMinimumInEur * ratePerEur;

        return CurrencyRules.RoundUp(Math.Max(fee, minimum));
    }

    /// <summary>The fee a premium user pays for a withdrawal or an outgoing transfer.</summary>
    public static decimal PremiumOutgoing(decimal amount, decimal ratePerEur, WeeklyUsage usage)
    {
        var chargedAmount = amount;

        // While fewer than five operations were made this week, all of them are among the first five,
        // so their total is exactly the allowance already used.
        if (usage.OperationCount < OperationsWithAllowancePerWeek)
        {
            var remainingInEur = Math.Max(0m, WeeklyAllowanceInEur - usage.AmountInEur);
            chargedAmount = Math.Max(0m, amount - remainingInEur * ratePerEur);
        }

        return CurrencyRules.RoundUp(chargedAmount * PremiumFeePercent / 100);
    }
}
