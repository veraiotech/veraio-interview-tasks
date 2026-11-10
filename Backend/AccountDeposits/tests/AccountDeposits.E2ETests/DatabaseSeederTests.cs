using AccountDeposits.Api.Models;
using AccountDeposits.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccountDeposits.E2ETests;

[Collection(ApiCollection.Name)]
public sealed class DatabaseSeederTests(AccountDepositsApiFactory factory)
{
    [Fact]
    public async Task Seed_creates_predefined_data_and_can_run_again()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // The database is shared, so start from empty instead of relying on what other tests left.
        await db.Database.ExecuteSqlRawAsync("TRUNCATE operations, accounts, users", cancellationToken);

        await DatabaseSeeder.SeedAsync(db, cancellationToken);
        await DatabaseSeeder.SeedAsync(db, cancellationToken);

        var users = await db.Users.ToListAsync(cancellationToken);
        var accounts = await db.Accounts.ToListAsync(cancellationToken);

        // SeedData is what the other tests rely on, so this test proves it matches the real seed.
        Assert.Equal(
            [SeedData.BasicUser, SeedData.PremiumUser, SeedData.LowBalanceUser],
            users.Select(user => user.Id).Order());
        Assert.Equal(UserType.Basic, users.Single(user => user.Id == SeedData.BasicUser).Type);
        Assert.Equal(UserType.Premium, users.Single(user => user.Id == SeedData.PremiumUser).Type);
        Assert.Equal(UserType.Basic, users.Single(user => user.Id == SeedData.LowBalanceUser).Type);

        Assert.Equal(
            [
                (SeedData.BasicEur, SeedData.BasicUser, Currency.EUR),
                (SeedData.BasicUsd, SeedData.BasicUser, Currency.USD),
                (SeedData.BasicGbp, SeedData.BasicUser, Currency.GBP),
                (SeedData.PremiumEur, SeedData.PremiumUser, Currency.EUR),
                (SeedData.PremiumUsd, SeedData.PremiumUser, Currency.USD),
                (SeedData.PremiumGbp, SeedData.PremiumUser, Currency.GBP),
                (SeedData.LowBalanceEur, SeedData.LowBalanceUser, Currency.EUR),
            ],
            accounts.OrderBy(account => account.Id).Select(account => (account.Id, account.UserId, account.Currency)));

        Assert.Equal(100.00m, accounts.Single(account => account.Id == SeedData.LowBalanceEur).Balance);
        Assert.All(
            accounts.Where(account => account.Id != SeedData.LowBalanceEur),
            account => Assert.Equal(SeedData.StartingBalance, account.Balance));
    }
}
