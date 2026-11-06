namespace AccountDeposits.Api.Errors;

/// <summary>A refused request. <see cref="ApiExceptionHandler"/> turns it into the shared error response.</summary>
public sealed class ApiException(int statusCode, string code, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public string Code { get; } = code;
}
