using AccountDeposits.Api.Dtos;
using AccountDeposits.Api.Errors;
using AccountDeposits.Api.Models;
using AccountDeposits.Api.Persistence;
using AccountDeposits.Api.Services.Fees;
using AccountDeposits.Api.Services.Money;
using Microsoft.EntityFrameworkCore;

namespace AccountDeposits.Api.Services;

public sealed class OperationService(AppDbContext db, IExchangeRateProvider rateProvider, TimeProvider timeProvider)
{
    /// <summary>Far above real operations and far below what numeric(18,2) can hold.</summary>
    private const decimal MaxAmount = 1_000_000_000.00m;

    public async Task<OperationResponse> DepositAsync(long userId, long accountId, decimal amount, CancellationToken cancellationToken)
    {
        EnsureValidAmount(amount);
        var currency = await GetAccountCurrencyAsync(userId, accountId, cancellationToken);
        var ratePerEur = await GetRatePerEurAsync(currency, cancellationToken);
        var fee = FeeCalculator.Deposit(amount, ratePerEur);

        return await InTransactionAsync([userId], async () =>
        {
            var account = await db.Accounts.SingleAsync(account => account.Id == accountId, cancellationToken);
            account.Balance += amount - fee;

            var operation = Record(account.Id, OperationType.Deposit, amount, fee, account.Balance, ratePerEur);
            await db.SaveChangesAsync(cancellationToken);

            return new OperationResponse(operation.Reference, fee, account.Currency, account.Balance);
        }, cancellationToken);
    }

    /// <summary>
    /// Runs <paramref name="work"/> in one transaction after locking the given users' rows. Every balance change
    /// must lock the account owner's row, so a transfer locks both the sender and the receiver. Locks are taken in
    /// id order, so two transactions cannot deadlock. Concurrent operations of the same user therefore run one after
    /// another. Any exception rolls everything back, so a refused operation leaves no trace.
    /// </summary>
    /// 
    private async Task<T> InTransactionAsync<T>(long[] userIds, Func<Task<T>> work, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync(
            $"SELECT id FROM users WHERE id = ANY({userIds}) ORDER BY id FOR UPDATE", cancellationToken);

        var result = await work();

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private Operation Record(
        long accountId, OperationType type, decimal amount, decimal fee, decimal balanceAfter, decimal ratePerEur)
    {
        var occurredAt = timeProvider.GetUtcNow();
        var operation = new Operation
        {
            Reference = Guid.CreateVersion7(occurredAt),
            AccountId = accountId,
            Type = type,
            Amount = amount,
            Fee = fee,
            BalanceAfter = balanceAfter,
            RatePerEur = ratePerEur,
            OccurredAt = occurredAt,
        };

        db.Operations.Add(operation);
        return operation;
    }

    private async Task<Currency> GetAccountCurrencyAsync(long userId, long accountId, CancellationToken cancellationToken)
    {
        // Not tracked, so the locked transaction below reads the account again instead of reusing this stale copy.
        var account = await db.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.Id == accountId && account.UserId == userId, cancellationToken);

        return account?.Currency ?? throw new ApiException(
            StatusCodes.Status404NotFound, "ACCOUNT_NOT_FOUND", $"Account {accountId} does not exist for user {userId}.");
    }

    /// <summary>EUR accounts need no conversion, so they keep working when the rates are unavailable.</summary>
    private async Task<decimal> GetRatePerEurAsync(Currency currency, CancellationToken cancellationToken) =>
        currency == Currency.EUR ? 1m : (await rateProvider.GetRatesAsync(cancellationToken)).PerEur(currency);

    private static void EnsureValidAmount(decimal amount)
    {
        if (!CurrencyRules.IsValidAmount(amount) || amount > MaxAmount)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "INVALID_AMOUNT",
                FormattableString.Invariant($"Amount must be positive, in whole cents and at most {MaxAmount:N2}."));
        }
    }
}
