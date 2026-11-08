namespace AccountDeposits.Api.Models;

/// <summary>
/// One completed operation on one account. A transfer is two rows, one per account, sharing a <see cref="Reference"/>.
/// Rows are only ever added, never changed.
/// </summary>
public sealed class Operation
{
    public long Id { get; init; }

    public required Guid Reference { get; init; }

    public required long AccountId { get; init; }

    public Account Account { get; init; } = null!;

    public required OperationType Type { get; init; }

    public required decimal Amount { get; init; }

    public required decimal Fee { get; init; }

    public required decimal BalanceAfter { get; init; }

    /// <summary>Units of the account currency per 1 EUR at the time of the operation; 1 for EUR accounts.</summary>
    public required decimal RatePerEur { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }
}
