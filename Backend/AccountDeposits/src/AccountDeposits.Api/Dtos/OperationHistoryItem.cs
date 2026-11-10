using AccountDeposits.Api.Models;

namespace AccountDeposits.Api.Dtos;

/// <summary>
/// One operation in a user's history. Amount, fee and balance are in <see cref="Currency"/>, the currency of
/// <see cref="AccountId"/>. <see cref="CounterpartyAccountId"/> is the other account of a transfer, otherwise null.
/// </summary>
public sealed record OperationHistoryItem(
    Guid Reference,
    OperationType Type,
    long AccountId,
    decimal Amount,
    Currency Currency,
    decimal Fee,
    decimal BalanceAfter,
    DateTimeOffset OccurredAt,
    long? CounterpartyAccountId);
