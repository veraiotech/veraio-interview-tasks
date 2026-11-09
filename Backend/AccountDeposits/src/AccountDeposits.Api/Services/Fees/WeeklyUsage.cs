namespace AccountDeposits.Api.Services.Fees;

/// <summary>The user's withdrawals and outgoing transfers so far this week: how many, and their total in EUR.</summary>
public sealed record WeeklyUsage(int OperationCount, decimal AmountInEur);
