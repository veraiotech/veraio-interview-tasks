using AccountDeposits.Api.Models;

namespace AccountDeposits.Api.Dtos;

/// <summary>
/// The outcome of a transfer for the sender: the fee and balance of the sender's account, in <see cref="Currency"/>,
/// and the amount the receiver got, in <see cref="ReceivedCurrency"/>.
/// </summary>
public sealed record TransferResponse(
    Guid Reference, decimal Fee, Currency Currency, decimal BalanceAfter, decimal ReceivedAmount, Currency ReceivedCurrency);
