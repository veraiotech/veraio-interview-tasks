using AccountDeposits.Api.Dtos;
using AccountDeposits.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccountDeposits.Api.Controllers;

[ApiController]
[Route("users/{userId}/accounts/{accountId}")]
public sealed class OperationsController(OperationService operationService) : ControllerBase
{
    [HttpPost("deposits")]
    public Task<OperationResponse> Deposit(long userId, long accountId, DepositRequest request, CancellationToken cancellationToken) =>
        operationService.DepositAsync(userId, accountId, request.Amount, cancellationToken);

    [HttpPost("withdrawals")]
    public Task<OperationResponse> Withdraw(long userId, long accountId, WithdrawalRequest request, CancellationToken cancellationToken) =>
        operationService.WithdrawAsync(userId, accountId, request.Amount, cancellationToken);
}
