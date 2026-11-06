using AccountDeposits.Api.Models;

namespace AccountDeposits.Api.Dtos;

public sealed record AccountResponse(long Id, Currency Currency, decimal Balance);
