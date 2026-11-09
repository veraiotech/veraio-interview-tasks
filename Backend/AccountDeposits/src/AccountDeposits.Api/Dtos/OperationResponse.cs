using AccountDeposits.Api.Models;

namespace AccountDeposits.Api.Dtos;

/// <summary>The outcome of a deposit or withdrawal. The fee is charged in <see cref="Currency"/>.</summary>
public sealed record OperationResponse(Guid Reference, decimal Fee, Currency Currency, decimal BalanceAfter);
