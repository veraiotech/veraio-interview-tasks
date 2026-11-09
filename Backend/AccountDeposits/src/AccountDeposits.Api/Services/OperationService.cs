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
        var (currency, _) = await GetAccountDetailsAsync(userId, accountId, cancellationToken);
        var ratePerEur = await GetRatePerEurAsync(currency, cancellationToken);
        var fee = FeeCalculator.Deposit(amount, ratePerEur);

        return await InTransactionAsync([userId], async () =>
        {
            var account = await db.Accounts.SingleAsync(account => account.Id == accountId, cancellationToken);
            account.Balance += amount - fee;

            var operation = AddOperation(
                account.Id, OperationType.Deposit, amount, fee, account.Balance, ratePerEur, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);

            return new OperationResponse(operation.Reference, fee, account.Currency, account.Balance);
        }, cancellationToken);
    }

    public async Task<OperationResponse> WithdrawAsync(long userId, long accountId, decimal amount, CancellationToken cancellationToken)
    {
        EnsureValidAmount(amount);
        var (currency, userType) = await GetAccountDetailsAsync(userId, accountId, cancellationToken);
        var ratePerEur = await GetRatePerEurAsync(currency, cancellationToken);

        return await InTransactionAsync([userId], async () =>
        {
            // One timestamp for the whole operation, so the week used for the fee is the week it is recorded in.
            var now = timeProvider.GetUtcNow();
            var fee = userType == UserType.Basic
                ? FeeCalculator.BasicOutgoing(amount, ratePerEur)
                : FeeCalculator.PremiumOutgoing(amount, ratePerEur, await GetWeeklyUsageAsync(userId, now, cancellationToken));

            var account = await db.Accounts.SingleAsync(account => account.Id == accountId, cancellationToken);
            EnsureSufficientBalance(account, amount + fee);
            account.Balance -= amount + fee;

            var operation = AddOperation(account.Id, OperationType.Withdrawal, amount, fee, account.Balance, ratePerEur, now);
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
    private async Task<T> InTransactionAsync<T>(long[] userIds, Func<Task<T>> work, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync(
            $"SELECT id FROM users WHERE id = ANY({userIds}) ORDER BY id FOR UPDATE", cancellationToken);

        var result = await work();

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private Operation AddOperation(
        long accountId, OperationType type, decimal amount, decimal fee, decimal balanceAfter, decimal ratePerEur,
        DateTimeOffset occurredAt)
    {
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

    /// <summary>
    /// Reads only values that never change. The balance is read again inside the locked transaction,
    /// so this lookup can safely run before it.
    /// </summary>
    private async Task<(Currency Currency, UserType UserType)> GetAccountDetailsAsync(
        long userId, long accountId, CancellationToken cancellationToken)
    {
        var account = await db.Accounts
            .Where(account => account.Id == accountId && account.UserId == userId)
            .Select(account => new { account.Currency, account.User.Type })
            .SingleOrDefaultAsync(cancellationToken);

        return account is null
            ? throw new ApiException(
                StatusCodes.Status404NotFound, "ACCOUNT_NOT_FOUND", $"Account {accountId} does not exist for user {userId}.")
            : (account.Currency, account.Type);
    }

    /// <summary>
    /// The user's withdrawals and outgoing transfers in the current calendar week, Monday 00:00 to Sunday 23:59:59 UTC.
    /// Both queries run inside the locked transaction, so no operation of the user can be recorded between them.
    /// </summary>
    private async Task<WeeklyUsage> GetWeeklyUsageAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var weekStart = StartOfWeek(now);
        var nextWeekStart = weekStart.AddDays(7);

        var operations = db.Operations.Where(operation =>
            operation.Account.UserId == userId
            && (operation.Type == OperationType.Withdrawal || operation.Type == OperationType.TransferOut)
            && operation.OccurredAt >= weekStart
            && operation.OccurredAt < nextWeekStart);

        var count = await operations.CountAsync(cancellationToken);
        var amountInEur = await operations.SumAsync(operation => operation.Amount / operation.RatePerEur, cancellationToken);

        return new WeeklyUsage(count, amountInEur);
    }

    private static DateTimeOffset StartOfWeek(DateTimeOffset now)
    {
        var today = now.UtcDateTime.Date;
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        return new DateTimeOffset(today.AddDays(-daysSinceMonday), TimeSpan.Zero);
    }

    private static void EnsureSufficientBalance(Account account, decimal debit)
    {
        if (debit > account.Balance)
        {
            throw new ApiException(
                StatusCodes.Status422UnprocessableEntity,
                "INSUFFICIENT_FUNDS",
                FormattableString.Invariant(
                    $"Amount plus fee is {debit:N2} {account.Currency}, but the balance is {account.Balance:N2} {account.Currency}."));
        }
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
