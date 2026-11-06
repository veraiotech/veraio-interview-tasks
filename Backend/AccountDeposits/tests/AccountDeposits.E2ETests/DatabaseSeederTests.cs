using AccountDeposits.Api.Models;
using AccountDeposits.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccountDeposits.E2ETests;

public sealed class DatabaseSeederTests(AccountDepositsApiFactory factory) : IClassFixture<AccountDepositsApiFactory>
{
    [Fact]
    public async Task Seed_creates_predefined_data_and_can_run_again()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await DatabaseSeeder.SeedAsync(db, cancellationToken);
        await DatabaseSeeder.SeedAsync(db, cancellationToken);

        var users = await db.Users.ToListAsync(cancellationToken);
        var accounts = await db.Accounts.ToListAsync(cancellationToken);

        Assert.Equal([1, 2, 3], users.Select(user => user.Id).Order());
        Assert.Equal([101, 102, 103, 201, 202, 203, 301], accounts.Select(account => account.Id).Order());
        Assert.Contains(users, user => user.Type == UserType.Basic);
        Assert.Contains(users, user => user.Type == UserType.Premium);
        Assert.Equal(3, accounts.Select(account => account.Currency).Distinct().Count());
        Assert.Equal(100.00m, accounts.Single(account => account.Id == 301).Balance);
    }
}
