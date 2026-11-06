namespace AccountDeposits.Api.Errors;

public static class ErrorHandlingExtensions
{
    /// <summary>Gives every error response the same ProblemDetails shape, with a <c>code</c> field.</summary>
    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions.TryAdd("code", context.ProblemDetails.Status switch
            {
                StatusCodes.Status400BadRequest => "VALIDATION_FAILED",
                StatusCodes.Status404NotFound => "NOT_FOUND",
                StatusCodes.Status405MethodNotAllowed => "METHOD_NOT_ALLOWED",
                StatusCodes.Status415UnsupportedMediaType => "UNSUPPORTED_MEDIA_TYPE",
                StatusCodes.Status500InternalServerError => "INTERNAL_ERROR",
                _ => "ERROR",
            }));

        return services;
    }
}
