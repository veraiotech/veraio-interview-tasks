namespace AccountDeposits.E2ETests;

/// <summary>The users and accounts that DatabaseSeeder creates, so tests can name them instead of using bare ids.</summary>
public static class SeedData
{
    public const long BasicUser = 1;
    public const long PremiumUser = 2;
    public const long LowBalanceUser = 3;

    public const long BasicEur = 101;
    public const long BasicUsd = 102;
    public const long BasicGbp = 103;

    public const long PremiumEur = 201;
    public const long PremiumUsd = 202;
    public const long PremiumGbp = 203;

    public const long LowBalanceEur = 301;

    /// <summary>The balance of every seeded account except <see cref="LowBalanceEur"/>, which starts at 100.00.</summary>
    public const decimal StartingBalance = 100_000.00m;
}
