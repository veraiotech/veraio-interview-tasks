using AccountDeposits.Api.Dtos;
using AccountDeposits.Api.Errors;
using AccountDeposits.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccountDeposits.Api.Services;

public sealed class AccountService(AppDbContext db)
{
    public async Task<List<AccountResponse>> GetAccountsAsync(long userId, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(user => user.Id == userId, cancellationToken))
        {
            throw new ApiException(StatusCodes.Status404NotFound, "USER_NOT_FOUND", $"User {userId} does not exist.");
        }

        return await db.Accounts
            .Where(account => account.UserId == userId)
            .OrderBy(account => account.Id)
            .Select(account => new AccountResponse(account.Id, account.Currency, account.Balance))
            .ToListAsync(cancellationToken);
    }
}
