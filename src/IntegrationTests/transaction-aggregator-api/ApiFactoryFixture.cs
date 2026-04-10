using Infrastructure.TransactionDb;
using Infrastructure.Webhooks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace IntegrationTests.transaction_aggregator_api;
/// <summary>
/// Boots the full ASP.NET Core pipeline against a real Postgres Testcontainer.
/// Shared across all tests in the ApiCollection to avoid redundant container startups.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("transaction_aggregator_api_tests")
        .WithUsername("postgres")
        .WithPassword("testpassword")
        .Build();
 
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }
 
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace the real DbContext with one pointing at the test container
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<TransactionDbContext>));
 
            if (descriptor is not null)
                services.Remove(descriptor);
 
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_container.GetConnectionString());
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();
 
            services.AddDbContext<TransactionDbContext>(options =>
                options.UseNpgsql(dataSource));

            // Remove all Hangfire hosted services during tests
            var hangfireServices = services
                .Where(d => d.ImplementationType?.Namespace?.Contains("Hangfire") == true)
                .ToList();

            foreach (var s in hangfireServices)
                services.Remove(s);
            
            
            // Replace webhook options with a known test secret
            var webhookDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(WebhookOptions));
 
            if (webhookDescriptor is not null)
                services.Remove(webhookDescriptor);
 
            services.AddSingleton(new WebhookOptions
            {
                Secrets = new Dictionary<string, string>
                {
                    ["test-source"] = "test-secret"
                }
            });
 
            // Apply migrations on startup
            var sp = services.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
            db.Database.Migrate();
        });
    }
 
    public new async Task DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }
}
 
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
