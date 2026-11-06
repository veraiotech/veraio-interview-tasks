namespace AccountDeposits.Api.Models;

public sealed class User
{
    public required long Id { get; init; }

    public required string Name { get; init; }

    public required UserType Type { get; init; }

    public ICollection<Account> Accounts { get; } = [];
}
