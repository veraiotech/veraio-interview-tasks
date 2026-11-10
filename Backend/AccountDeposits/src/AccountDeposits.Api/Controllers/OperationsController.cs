using AccountDeposits.Api.Dtos;
using AccountDeposits.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccountDeposits.Api.Controllers;

[ApiController]
[Route("users/{userId}")]
public sealed class OperationsController(OperationService operationService) : ControllerBase
{
    [HttpPost("accounts/{accountId}/deposits")]
    public Task<OperationResponse> Deposit(long userId, long accountId, DepositRequest request, CancellationToken cancellationToken) =>
        operationService.DepositAsync(userId, accountId, request.Amount, cancellationToken);

    [HttpPost("accounts/{accountId}/withdrawals")]
    public Task<OperationResponse> Withdraw(long userId, long accountId, WithdrawalRequest request, CancellationToken cancellationToken) =>
        operationService.WithdrawAsync(userId, accountId, request.Amount, cancellationToken);

    [HttpPost("accounts/{accountId}/transfers")]
    public Task<TransferResponse> Transfer(long userId, long accountId, TransferRequest request, CancellationToken cancellationToken) =>
        operationService.TransferAsync(userId, accountId, request.ToAccountId, request.Amount, cancellationToken);

    [HttpGet("operations")]
    public Task<List<OperationHistoryItem>> GetOperations(long userId, CancellationToken cancellationToken) =>
        operationService.GetHistoryAsync(userId, cancellationToken);
}
