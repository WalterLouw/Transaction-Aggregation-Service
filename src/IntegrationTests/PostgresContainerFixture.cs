using Infrastructure.TransactionDb;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace IntegrationTests.Fixtures;

/// <summary>
/// Starts a single Postgres container shared across all tests in the collection.
/// Each test gets a freshly migrated DbContext via CreateDbContextAsync().
/// </summary>
public class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("transaction_aggregator_tests")
        .WithUsername("postgres")
        .WithPassword("testpassword")
        .Build();
 
    public string ConnectionString => _container.GetConnectionString();
 
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
 
        // Apply migrations so the schema exists before any test runs
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }
 
    public async Task DisposeAsync() => await _container.DisposeAsync();
 
    public TransactionDbContext CreateDbContext()
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();
 
        var options = new DbContextOptionsBuilder<TransactionDbContext>()
            .UseNpgsql(dataSource)
            .Options;
 
        return new TransactionDbContext(options);
    }
}
 
/// <summary>
/// xUnit collection definition — ensures the container is shared across test classes.
/// </summary>
[CollectionDefinition(Name)]
public class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "Postgres";
}