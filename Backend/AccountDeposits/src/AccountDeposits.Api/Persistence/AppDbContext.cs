using AccountDeposits.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountDeposits.Api.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Account> Accounts => Set<Account>();

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
    }
}
