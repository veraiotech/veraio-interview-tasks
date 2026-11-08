using AccountDeposits.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountDeposits.Api.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Operation> Operations => Set<Operation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.ToTable(table => table.HasCheckConstraint("ck_users_type", "type IN ('Basic', 'Premium')"));
            user.Property(entity => entity.Id).ValueGeneratedNever();
            user.Property(entity => entity.Type).HasConversion<string>();
        });

        modelBuilder.Entity<Account>(account =>
        {
            account.ToTable(table =>
            {
                table.HasCheckConstraint("ck_accounts_currency", "currency IN ('EUR', 'USD', 'GBP')");
                table.HasCheckConstraint("ck_accounts_balance_non_negative", "balance >= 0");
            });
            account.Property(entity => entity.Id).ValueGeneratedNever();
            account.Property(entity => entity.Currency).HasConversion<string>();
            account.Property(entity => entity.Balance).HasPrecision(18, 2).HasDefaultValue(0m);
            account.HasOne(entity => entity.User)
                .WithMany(user => user.Accounts)
                .HasForeignKey(entity => entity.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Operation>(operation =>
        {
            operation.ToTable(table =>
            {
                table.HasCheckConstraint("ck_operations_type", "type IN ('Deposit', 'Withdrawal', 'TransferOut', 'TransferIn')");
                table.HasCheckConstraint("ck_operations_amount_positive", "amount > 0");
                table.HasCheckConstraint("ck_operations_fee_non_negative", "fee >= 0");
                table.HasCheckConstraint("ck_operations_balance_after_non_negative", "balance_after >= 0");
                table.HasCheckConstraint("ck_operations_rate_per_eur_positive", "rate_per_eur > 0");
            });
            operation.Property(entity => entity.Type).HasConversion<string>();
            operation.Property(entity => entity.Amount).HasPrecision(18, 2);
            operation.Property(entity => entity.Fee).HasPrecision(18, 2);
            operation.Property(entity => entity.BalanceAfter).HasPrecision(18, 2);
            operation.Property(entity => entity.RatePerEur).HasPrecision(18, 6);
            operation.HasIndex(entity => new { entity.Reference, entity.AccountId }).IsUnique();
            operation.HasIndex(entity => new { entity.AccountId, entity.OccurredAt, entity.Id });
            operation.HasOne(entity => entity.Account)
                .WithMany()
                .HasForeignKey(entity => entity.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
