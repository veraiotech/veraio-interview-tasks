using AccountDeposits.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountDeposits.Api.Persistence;

public static class DatabaseSeeder
{
    private const decimal StartingBalance = 100_000.00m;

    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Users.AddRange(
            new User { Id = 1, Name = "Basic User", Type = UserType.Basic },
            new User { Id = 2, Name = "Premium User", Type = UserType.Premium },
            new User { Id = 3, Name = "Low Balance User", Type = UserType.Basic });

        db.Accounts.AddRange(
            new Account { Id = 101, UserId = 1, Currency = Currency.EUR, Balance = StartingBalance },
            new Account { Id = 102, UserId = 1, Currency = Currency.USD, Balance = StartingBalance },
            new Account { Id = 103, UserId = 1, Currency = Currency.GBP, Balance = StartingBalance },
            new Account { Id = 201, UserId = 2, Currency = Currency.EUR, Balance = StartingBalance },
            new Account { Id = 202, UserId = 2, Currency = Currency.USD, Balance = StartingBalance },
            new Account { Id = 203, UserId = 2, Currency = Currency.GBP, Balance = StartingBalance },
            new Account { Id = 301, UserId = 3, Currency = Currency.EUR, Balance = 100.00m });

        await db.SaveChangesAsync(cancellationToken);
    }
}
