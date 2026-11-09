namespace AccountDeposits.Api.Models;

public sealed class Account
{
    public required long Id { get; init; }

    public required long UserId { get; init; }

    public User User { get; init; } = null!;

    public required Currency Currency { get; init; }

    public decimal Balance { get; set; }
}
