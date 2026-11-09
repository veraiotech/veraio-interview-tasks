namespace AccountDeposits.Api.Dtos;

public sealed record TransferRequest(long ToAccountId, decimal Amount);
