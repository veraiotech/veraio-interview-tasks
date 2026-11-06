using AccountDeposits.Api.Dtos;
using AccountDeposits.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccountDeposits.Api.Controllers;

[ApiController]
[Route("users/{userId}/accounts")]
public sealed class AccountsController(AccountService accountService) : ControllerBase
{
    [HttpGet]
    public Task<List<AccountResponse>> GetAccounts(long userId, CancellationToken cancellationToken) =>
        accountService.GetAccountsAsync(userId, cancellationToken);
}
