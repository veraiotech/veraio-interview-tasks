using AccountDeposits.Api.Models;
using AccountDeposits.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AccountDeposits.E2ETests;

public sealed class DatabaseSchemaTests(AccountDepositsApiFactory factory)
    : IClassFixture<AccountDepositsApiFactory>, IDisposable
{
    private const string CheckViolation = "23514";

    private readonly IServiceScope _scope = factory.Services.CreateScope();

    private AppDbContext Db => _scope.ServiceProvider.GetRequiredService<AppDbContext>();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => _scope.Dispose();

    [Fact]
    public async Task Migrations_create_the_schema_on_an_empty_database()
    {
        var tables = await Db.Database
            .SqlQuery<string>($"SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public'")
            .ToListAsync(CancellationToken);

        Assert.Contains("users", tables);
        Assert.Contains("accounts", tables);
    }

    [Fact]
    public async Task Balance_cannot_be_negative()
    {
        Db.Users.Add(new User { Id = 1, Name = "Test user", Type = UserType.Basic });
        Db.Accounts.Add(new Account { Id = 1, UserId = 1, Currency = Currency.EUR, Balance = -0.01m });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => Db.SaveChangesAsync(CancellationToken));

        var postgres = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(CheckViolation, postgres.SqlState);
        Assert.Equal("ck_accounts_balance_non_negative", postgres.ConstraintName);
    }
}
