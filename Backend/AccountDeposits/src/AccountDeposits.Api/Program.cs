using System.Text.Json.Serialization;
using AccountDeposits.Api.Configuration;
using AccountDeposits.Api.Errors;
using AccountDeposits.Api.Persistence;
using AccountDeposits.Api.Services;
using AccountDeposits.Api.Services.Money;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// .env is optional and real environment variables override it. It is loaded in every environment
// (dotnet ef and published builds run as Production) except Testing, where tests supply all settings.
if (!builder.Environment.IsEnvironment("Testing"))
{
    Env.NoClobber().TraversePath().Load();
    builder.Configuration.AddEnvironmentVariables();
}

// Stops the service at startup when a required setting is missing.
var connectionString = DatabaseSettings.GetConnectionString(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options => options
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention());
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<OperationService>();
builder.Services.AddHttpClient<IExchangeRateProvider, FrankfurterExchangeRateProvider>(client =>
{
    client.BaseAddress = FrankfurterExchangeRateProvider.BaseAddress;
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddErrorHandling();
builder.Services.AddOpenApi();

var app = builder.Build();

if (args.Contains("seed"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await DatabaseSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
