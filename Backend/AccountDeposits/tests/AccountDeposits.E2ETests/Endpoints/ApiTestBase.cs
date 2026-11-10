using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AccountDeposits.Api.Dtos;
using AccountDeposits.Api.Models;
using AccountDeposits.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccountDeposits.E2ETests.Endpoints;

/// <summary>Starts every test from the freshly seeded data and gives short helpers for the endpoints.</summary>
public abstract class ApiTestBase(AccountDepositsApiFactory factory) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected AccountDepositsApiFactory Factory { get; } = factory;

    protected HttpClient Client { get; } = factory.CreateClient();

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await Factory.ResetAsync();

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }

    protected Task<HttpResponseMessage> DepositAsync(long userId, long accountId, decimal amount) =>
        Client.PostAsJsonAsync($"users/{userId}/accounts/{accountId}/deposits", new { amount }, CancellationToken);

    protected Task<HttpResponseMessage> WithdrawAsync(long userId, long accountId, decimal amount) =>
        Client.PostAsJsonAsync($"users/{userId}/accounts/{accountId}/withdrawals", new { amount }, CancellationToken);

    protected Task<HttpResponseMessage> TransferAsync(long userId, long accountId, long toAccountId, decimal amount) =>
        Client.PostAsJsonAsync($"users/{userId}/accounts/{accountId}/transfers", new { toAccountId, amount }, CancellationToken);

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        await AssertStatusAsync(response, HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, CancellationToken))!;
    }

    /// <summary>Checks the status and the error code, and returns the error body for any further checks.</summary>
    protected static async Task<JsonElement> AssertErrorAsync(
        Task<HttpResponseMessage> request, HttpStatusCode status, string errorCode)
    {
        var response = await request;
        await AssertStatusAsync(response, status);

        var error = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(errorCode, error.GetProperty("code").GetString());
        return error;
    }

    /// <summary>On an unexpected status, fails with the response body, so the reason for it is visible.</summary>
    private static async Task AssertStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync(CancellationToken);
            Assert.Fail($"Expected {(int)expected} but got {(int)response.StatusCode}: {body}");
        }
    }

    protected async Task<List<AccountResponse>> GetAccountsAsync(long userId) =>
        (await Client.GetFromJsonAsync<List<AccountResponse>>($"users/{userId}/accounts", JsonOptions, CancellationToken))!;

    protected async Task<decimal> GetBalanceAsync(long userId, long accountId) =>
        (await GetAccountsAsync(userId)).Single(account => account.Id == accountId).Balance;

    protected async Task<List<Operation>> GetOperationsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Operations.OrderBy(operation => operation.Id).ToListAsync(CancellationToken);
    }
}
