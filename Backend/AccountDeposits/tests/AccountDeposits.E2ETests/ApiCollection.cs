namespace AccountDeposits.E2ETests;

/// <summary>
/// Every test that needs the API or the database shares one instance and one PostgreSQL container, so they run
/// one at a time. Endpoint tests reset the data before each test; the other tests do not rely on what is there.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<AccountDepositsApiFactory>
{
    public const string Name = "Api";
}
