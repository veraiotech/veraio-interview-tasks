using AccountDeposits.Api.Configuration;
using AccountDeposits.Api.Persistence;
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
builder.Services.AddOpenApi();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
}

app.Run();
