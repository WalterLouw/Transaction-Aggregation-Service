using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Infrastructure.TransactionDb;

/// <summary>
/// Used by EF Core tooling (dotnet ef migrations) at design time.
/// Not used at runtime — the real DbContext is registered via DI in InfrastructureServiceExtensions.
/// </summary>
public class TransactionDbContextFactory : IDesignTimeDbContextFactory<TransactionDbContext>
{
    public TransactionDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../transaction-aggregator-api"))
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
 
         
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(configuration.GetConnectionString("Postgres"));
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();
        
        var optionsBuilder = new DbContextOptionsBuilder<TransactionDbContext>();
        optionsBuilder.UseNpgsql(dataSource);
 
        return new TransactionDbContext(optionsBuilder.Options);
    }
}
