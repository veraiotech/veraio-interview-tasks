# Deployment guide

How the Account Deposits service is built, configured and operated. What the API does is described in [FEATURES.md](FEATURES.md).

## 1. Overview and tech stack

A REST service that holds the balances of predefined users and accounts in PostgreSQL and charges commission on deposits, withdrawals and transfers. Fees are calculated with the European Central Bank reference rates, fetched from the [Frankfurter API](https://frankfurter.dev).

| Part | Technology |
|---|---|
| Language | C# 14 |
| Framework | .NET 10, ASP.NET Core with controllers |
| Database | PostgreSQL 17, run with Docker Compose |
| Data access | Entity Framework Core 10 with Npgsql, migrations, snake_case names (`EFCore.NamingConventions`) |
| Configuration | Environment variables, optionally from a `.env` file (`DotNetEnv`) |
| Exchange rates | Frankfurter, through a typed `HttpClient` |
| Errors | `ProblemDetails` with an extra `code` field |
| Tests | xUnit v3, `WebApplicationFactory`, Testcontainers for PostgreSQL, `FakeTimeProvider` |

## 2. Prerequisites

| Tool | Version | Used for |
|---|---|---|
| .NET SDK | 10.0.403 or a later 10.0.4xx patch (pinned in `global.json`) | Build, run, test, migrations |
| Docker with Docker Compose | Tested with Docker Engine 29 and Docker Compose 5.6 | The local database and the test database |
| curl and jq | any | Trying the endpoints and formatting the JSON responses (optional) |

The `dotnet-ef` tool is not installed globally. It is a local tool in `.config/dotnet-tools.json` and is restored with `dotnet tool restore`.

## 3. Environment variables

### File

The service reads `.env` from the directory it is started in or from the nearest parent directory. It is loaded automatically, in every environment except `Testing`, which the tests use so that a developer's `.env` never affects them. Run the commands in this guide from `Backend/AccountDeposits`, where `.env` lives.

`docker-compose.yml` reads the same `.env` for the database container.

### Template

`.env.example` is committed. Create your own copy, which git ignores:

```bash
cp .env.example .env
```

The template works as it is with the Docker Compose database.

### Structure

One `NAME=value` per line, and lines starting with `#` are comments. Names are upper case, as in `POSTGRES_DB`. Wrap a value in single quotes if it contains `$`, `#` or spaces, because quotes and `$` are interpreted.

### Overrides

Every variable can also be set in the shell or the container environment. A real environment variable wins over the same name in `.env`:

```bash
POSTGRES_PORT=5432 dotnet run --project src/AccountDeposits.Api
```

### Supported variables

| Variable | Required | Default | Description |
|---|---|---|---|
| `POSTGRES_HOST` | No | `localhost` | Database host. Read by the API only |
| `POSTGRES_PORT` | No | `5433` | Database port. Docker Compose publishes the container on this port |
| `POSTGRES_DB` | Yes | | Database name |
| `POSTGRES_USER` | Yes | | Database user |
| `POSTGRES_PASSWORD` | Yes | | Database password |
| `ASPNETCORE_ENVIRONMENT` | No | `Production` (`Development` with `dotnet run`) | ASP.NET Core environment. The OpenAPI document is served in every environment except `Production` |
| `ASPNETCORE_URLS` | No | `http://localhost:5080` with `dotnet run`, `http://localhost:5000` otherwise | The address the service listens on. With `dotnet run`, change it with `--urls`, for example `dotnet run --project src/AccountDeposits.Api --urls http://localhost:6000` |

### When a required variable is missing

The service stops at startup, before it listens for requests, with:

```text
Missing required configuration: POSTGRES_DB, POSTGRES_PASSWORD. Copy .env.example to .env or set them as environment variables.
```

The message lists every missing variable. A `POSTGRES_PORT` that is not a whole number also stops the service, with a message that names the variable.

## 4. Project structure

```text
Backend/AccountDeposits/
├── .config/dotnet-tools.json          the local dotnet-ef tool
├── docker-compose.yml                 the local PostgreSQL 17 database
├── .env.example                       the configuration template
├── src/AccountDeposits.Api/
│   ├── Program.cs                     configuration, services, middleware, the seed command
│   ├── Configuration/                 database settings read from the environment
│   ├── Controllers/                   HTTP endpoints: accounts and operations
│   ├── Dtos/                          request and response models
│   ├── Errors/                        ApiException and the shared error format
│   ├── Models/                        EF Core entities and enums
│   ├── Persistence/                   AppDbContext, migrations, DatabaseSeeder
│   └── Services/
│       ├── AccountService.cs          balances
│       ├── OperationService.cs        deposits, withdrawals, transfers, history, locking
│       ├── Fees/                      fee rules and the weekly allowance
│       └── Money/                     rounding, exchange rates, the Frankfurter client and its refresh
└── tests/
    ├── AccountDeposits.UnitTests/     fee and rounding rules, the rate store and refresh timing
    └── AccountDeposits.E2ETests/      the API against a real PostgreSQL container
```

### Layers

```text
Controllers  →  Services  →  Persistence (AppDbContext)  →  PostgreSQL
                   │
                   ├── Services/Fees    pure fee rules
                   └── Services/Money   rounding, rates
```

- **Controllers** only bind the request and call a service.
- **Services** hold the use cases: they validate, lock, calculate and save. They use `AppDbContext` directly, without a repository layer, because the context already plays that role.
- **Models** and **Dtos** are separate, so the database entities never leave the API.

### Business rules

- **Fees:** `Services/Fees/FeeCalculator.cs`. Pure functions with no database or HTTP, so every example from Task.md section 4 is a unit test.
- **Rounding, valid amounts, transfer credit and minimum:** `Services/Money/CurrencyRules.cs`.
- **Balance checks, locking, the weekly allowance query and the history:** `Services/OperationService.cs`.

### Exchange rates

- `FrankfurterClient` calls `https://api.frankfurter.dev/v1/latest?base=EUR&symbols=USD,GBP`.
- `ExchangeRateRefresher` is a background service. It fetches the rates at startup and daily at 16:00 UTC, and retries every 5 minutes after a failure or while Frankfurter does not have the day's rates yet.
- `ExchangeRateStore` keeps the latest rates in memory and is the `IExchangeRateProvider` that operations use. Operations never call Frankfurter themselves. Operations that stay in EUR need no rate at all.

**In tests** the background service and the store are removed, and `IExchangeRateProvider` is replaced by `FakeExchangeRateProvider`, which returns the Task.md rates (1 EUR = 1.10 USD = 0.85 GBP) and can act as if the rates were unavailable. The clock is a `FakeTimeProvider`, so the tests can move to the next week.

### One request end to end

`POST /users/1/accounts/102/transfers` with `{ "toAccountId": 203, "amount": 100.00 }`:

1. `OperationsController.Transfer` binds the route and the body and calls `OperationService.TransferAsync`.
2. The service validates the amount, reads the sender's and receiver's currencies and user types, refuses a self-transfer and takes both rates from `ExchangeRateStore`.
3. It calculates what the receiver gets and refuses the transfer if that is less than 0.01.
4. In one transaction it locks both users' rows with `SELECT ... FOR UPDATE` in id order, calculates the fee (reading the premium allowance if needed), checks the sender's balance, changes both balances and adds two rows to `operations` with one reference.
5. `SaveChanges` and the commit write everything at once. Any refusal throws an `ApiException`, which becomes a `ProblemDetails` response. A refusal inside the transaction also rolls it back, so nothing is saved.

## 5. Scripts

| Script | Command | Run from | Needs first | Safe to repeat | What it does |
|---|---|---|---|---|---|
| Restore tools | `dotnet tool restore` | `Backend/AccountDeposits` | | Yes | Installs the local `dotnet-ef` |
| Start database | `docker compose up -d --wait` | `Backend/AccountDeposits` | `.env` | Yes | Starts PostgreSQL 17 on port 5433, with data in a Docker volume, and waits until it accepts connections |
| Stop database | `docker compose down` | `Backend/AccountDeposits` | | Yes | Stops the database and keeps the data |
| Reset database | `docker compose down -v` | `Backend/AccountDeposits` | | Yes | Stops the database and **deletes all its data** |
| Build | `dotnet build` | `Backend/AccountDeposits` | | Yes | Builds the API and the tests |
| Migrate | `dotnet tool run dotnet-ef database update --project src/AccountDeposits.Api` | `Backend/AccountDeposits` | Running database, tools restored | Yes | Creates or updates the schema |
| Seed | `dotnet run --project src/AccountDeposits.Api -- seed` | `Backend/AccountDeposits` | Migrated database | Yes, it does nothing when users exist | Creates the predefined users and accounts, then exits |
| Start service | `dotnet run --project src/AccountDeposits.Api` | `Backend/AccountDeposits` | Migrated and seeded database | Yes | Listens on `http://localhost:5080`; stop with `Ctrl+C` |
| Test | `dotnet test` | `Backend/AccountDeposits` | Docker running | Yes | Runs all unit and end-to-end tests. The end-to-end tests start their own throwaway PostgreSQL container and never touch the local database |
| Unit tests only | `dotnet test --project tests/AccountDeposits.UnitTests` | `Backend/AccountDeposits` | | Yes | Runs the tests that need neither Docker nor a database |
| Format check | `dotnet format --verify-no-changes` | `Backend/AccountDeposits` | | Yes | Fails if the code does not follow the formatting rules |

The first migration logs one `fail` line about `__EFMigrationsHistory`. That is expected: EF Core looks for the history table before creating it.

The seed does not reset balances. To start again from the seeded data, reset the database and run the migration and the seed again.

### First-run sequence

From a fresh clone to a running, seeded service:

```bash
cd Backend/AccountDeposits
cp .env.example .env
docker compose up -d --wait
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/AccountDeposits.Api
dotnet run --project src/AccountDeposits.Api -- seed
dotnet run --project src/AccountDeposits.Api
```

Then, in another terminal:

```bash
curl -s http://localhost:5080/users/1/accounts
```

## 6. Technical decisions

| Decision | Alternatives | Why |
|---|---|---|
| One API project with folders by type: controllers, services, models, DTOs | Separate Domain, Application and Infrastructure projects | Easy to navigate for a service of this size; the folders keep the layers apart |
| Controllers | Minimal APIs | Familiar structure with clear grouping per resource |
| `DotNetEnv` and a static `DatabaseSettings` | The Options pattern, a custom `.env` parser | One group of settings read once at startup; far less code than a custom parser |
| `bigint` ids for users and accounts | `uuid` | Short ids are easy to use in requests and examples; the data is predefined, so guessable ids are not a concern. Operation references stay `uuid` |
| Enums stored as `text` with a `CHECK` constraint | PostgreSQL enums, integers | Readable in the database and changed with an ordinary migration |
| `numeric(18,2)` for money and `decimal` in C# | `double`; whole cents in a `bigint` | Exact to the cent with no conversions; `double` cannot hold amounts like 0.10 exactly, and whole cents would need converting on every input and output |
| One `operations` table with one row per affected account; a transfer is two rows with one reference | Separate tables per operation type, one row per transfer | The history of an account is one simple query, and each side keeps its own amount and balance |
| No `currency`, `user_id` or `counterparty_account_id` columns in `operations` | Storing them | Each one can be derived (from `accounts` or from the shared reference), so it would only repeat existing data |
| `rate_per_eur` stored on every operation | Not storing the rate | Every fee can be checked later, and the weekly allowance is calculated with the rate used at the time |
| The weekly allowance is derived from this week's withdrawals and outgoing transfers: their count and their total in EUR | An `allowance_used_eur` column or a separate counter table | One source of truth. While fewer than five operations were made, all of them are among the first five, so their total is exactly the allowance used |
| Users' rows locked with `SELECT ... FOR UPDATE`, in id order, inside one transaction | Optimistic concurrency, `SERIALIZABLE`, conditional `UPDATE` | The allowance is shared by all accounts of a user, so the user is the right thing to lock; id order means opposite transfers cannot deadlock; no retries are needed. The concurrency tests were checked to fail when the lock or the id order is removed |
| Fees calculated in the account currency, with the EUR limits converted into it | Converting the amount into EUR | The same result without dividing the amount by a rate, so no rounding noise appears |
| Rounding strips noise beyond 10 decimals first | Plain `Math.Round` | `decimal` keeps a finite number of digits, so conversions can leave values such as 0.50000000000000000000000001 that would otherwise round up to 0.51 |
| Exchange rates refreshed in the background, daily at 16:00 UTC, and kept in memory | Calling Frankfurter on every operation; also refreshing on demand when an operation finds no usable rates | ECB rates change once per working day; operations stay fast and keep working through a short Frankfurter outage |
| Rates count as unavailable when dated more than 5 days ago | No age limit, an age measured from the time of the last fetch | Covers weekends and holidays such as Easter, when the ECB publishes nothing from Thursday to Tuesday, and still refuses rates that are far out of date, whether Frankfurter is down or keeps returning old rates |
| On an ECB holiday on a weekday, the refresh retries every 5 minutes after 16:00 UTC until midnight | A calendar of ECB holidays | About 96 small requests on a few days a year; the "rates not current yet" information logs on those days are expected |
| One `ApiException` with the HTTP status and an error code | Separate exception types per error | The services only serve HTTP, so one way to refuse keeps the code short |
| `ProblemDetails` for every error, with a `code` field | A custom error body | The ASP.NET Core standard; framework errors such as invalid JSON get the same shape |
| `TimeProvider` for the current time | `DateTimeOffset.UtcNow` | Tests can fix the time and move to the next week |
| End-to-end tests against a real PostgreSQL in Testcontainers, sharing one container and reseeding before each test | EF Core InMemory or SQLite, one container per test class | Locks, `CHECK` constraints and PostgreSQL types behave as in production; one container keeps the suite simple |
